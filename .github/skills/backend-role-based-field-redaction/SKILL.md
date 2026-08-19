---
name: backend-role-based-field-redaction
description: "Use when a SmartGrader API response must show different fields to different roles — hiding test-case answers from students, or any field a teacher/admin may see but a student may not. Covers deciding the caller's role at the controller boundary and threading it into the Query, redacting the DTO inside the handler, why client-side hiding is not a control, and the leak paths that are easy to miss (list endpoints, notification feeds, and the AI prompt). USE FOR: 'hide X from students', 'redact a field per role', 'students can read the expected output', 'this endpoint returns too much for a student'. NOT for allow/deny on a whole endpoint (that is [Authorize] plus LessonAccess), and NOT for who owns which rows (that is the TeacherId ownership filter in the repositories)."
---

# Backend Role-Based Field Redaction

Some responses are legitimately readable by both a teacher and a student, but must not carry the
same **fields**. The established case: `TestCase.IsSample == false` means the test carries the answer
to the exercise, so a student gets the row's pass/fail and nothing else.

This is different from the two authorization mechanisms already in the codebase:

| Concern                            | Mechanism                                                  |
| ---------------------------------- | ---------------------------------------------------------- |
| May this caller reach the endpoint | `[Authorize(Roles = ...)]`, `LessonAccess`                 |
| Which rows may this caller see     | `TeacherId` / `StudentId` filters in the repository query  |
| **Which fields may they see**      | **this skill** — `TestVisibility`, applied in the handler  |

## When to Use

- A DTO returned to a student carries a field only a teacher should see.
- Adding a new endpoint that returns `AssignmentResponseDto` or `SubmissionResponseDto`.
- Reviewing a change to `TestCase`, `TestCaseResult`, or anything reaching
  [TestVisibility](../../../server/Application/Common/Authorization/TestVisibility.cs).

## Workflow

1. **Decide the role at the controller boundary**, never inside the handler. The handler must not
   touch `User`.

   ```csharp
   // StudentsController
   new GetSubmissionByIdQuery(studentId, submissionId, TeacherIdForSharedRead, !IsPrivilegedUser)
   ```

   ⚠️ **`TeacherIdForSharedRead is null` does NOT mean "student".** It is also null for an Admin
   (`OwnerScopeTeacherId` returns null so an Admin sees everything). The two correct signals are:

   | Signal                       | Where it works                                                        |
   | ---------------------------- | --------------------------------------------------------------------- |
   | `!IsPrivilegedUser`          | anywhere — the explicit one, pass it as a `bool IsStudentCaller` param |
   | `StudentId.HasValue`         | queries already carrying `int? StudentId` from `TryResolveSharedReadScope` |

   Where the query already carries `StudentId`, expose it as a documented derived property rather
   than adding a second parameter that can drift out of sync:

   ```csharp
   public record GetAssignmentByIdQuery(int LessonId, int AssignmentId, int? TeacherId, int? StudentId = null)
       : IRequest<AssignmentResponseDto>
   {
       public bool IsStudentCaller => StudentId.HasValue;
   }
   ```

2. **Redact in the handler, on the DTO, before returning** — one call, right at the `return`:

   ```csharp
   return TestVisibility.RedactTests(
       _mapper.Map<AssignmentResponseDto>(assignment),
       request.IsStudentCaller);
   ```

   Do **not** mutate the entity (e.g. `assignment.SetTests(filtered)`): `AssignmentRepository.GetByIdAsync`
   returns a *tracked* entity, so redaction would be one stray `SaveChangesAsync` away from deleting
   the teacher's test cases for real.

3. **Redact every path that returns the type, not just the one in the bug report.** The original
   report named two; there were five. Before calling it done, grep for the DTO:

   ```
   grep -rn "IReadOnlyList<SubmissionResponseDto>\|<SubmissionResponseDto>" server/Application/UseCases
   ```

   The ones that are easy to miss:
   - the **list** endpoint next to the by-id endpoint (`GetSubmissions`, `GetAssignments`)
   - **feeds** that reuse the same DTO for something unrelated (`GetRecentGradedSubmissions` — the
     notifications bell)
   - anything that puts the value in **free text a student reads**: `OpenAiFeedbackService` fed every
     test's `Input`/`Expected` into the prompt, and the model's answer is rendered to the student.
     A prompt is a leak path.

4. **Blank every field the hidden value can travel through, not just the obvious ones.** For a hidden
   test the student's own code runs on the hidden input, so `Actual` (stdout) and `Error` (stderr) can
   echo it back if she prints it. All four of `Input`/`Expected`/`Actual`/`Error` are cleared; only
   `Passed` survives, which is what keeps the "עברו 3 מתוך 5" summary honest without revealing
   anything.

5. **Fail closed on the flag itself.** `TestCase.IsSample` and `TestCaseResult.IsSample` default to
   `false`, so rows serialized before the field existed deserialize as *hidden*. A new visibility flag
   must default to the safe value, never to visible.

6. **Persist the flag on the result, don't re-derive it at read time.** `TestCaseResult.IsSample` is
   stored with the result rather than matched by index against `Assignment.Tests`, because the teacher
   can edit the test cases after the submission was graded — index matching would then expose a hidden
   row's details.

7. **Only then touch the client.** The client change is presentation (`isHidden` → render
   `בדיקה 3 · מוסתרת`, no expand toggle). It is never the control: the payload is already in the
   browser and readable in DevTools, so anything hidden only in an Angular template is not hidden.

## The Two Concrete Sites

| Path                          | What is redacted                                                        |
| ----------------------------- | ----------------------------------------------------------------------- |
| Assignment `Tests`            | non-sample `TestCaseDto` rows dropped entirely from the list            |
| Submission `TestResults`      | non-sample rows keep `Passed`; `Input`/`Expected`/`Actual`/`Error` blanked, `IsHidden = true` |

## Pitfalls

- Treating `TeacherId is null` as "student" — it is also an Admin, who then loses data they should see.
- Redacting only the by-id endpoint and leaving the list endpoint, the feed, or the AI prompt open.
- Mutating the tracked entity instead of the DTO.
- Defaulting a new visibility flag to `true` — every historical row instantly becomes public.
- Hiding in the Angular template and calling it fixed.

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the handler this redaction sits at the end of.
- [backend-automapper-profile-pattern](../backend-automapper-profile-pattern/SKILL.md) — `IsHidden` is a DTO-only field and is `.Ignore()`d in the profile.
- [client-student-area-pattern](../client-student-area-pattern/SKILL.md) — the student screens that consume the redacted payload.
