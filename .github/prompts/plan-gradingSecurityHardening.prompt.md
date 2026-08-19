# Plan: Grading Security & Reliability Hardening (Prompt 1 of 2)

## TL;DR

Students can currently read **every test case including the expected output** from a shared read endpoint, a
student can bypass the "one graded submission" rule by simply submitting again (both rows survive and the
dashboard averages both), a server restart silently destroys everything queued in Hangfire, and a student whose
code fails to compile is shown *"אין צורך לעשות דבר"* with **no way to fix and resubmit** even though the server
already supports it. This prompt closes all of that plus a set of guardrails and four silent bugs. It is
**self-contained** — the system is strictly better after this even if Prompt 2
([plan-gradingRequirementsEngine](plan-gradingRequirementsEngine.prompt.md)) never runs.

## User decisions (already confirmed)

- **Test visibility**: hidden by default; the teacher marks individual tests as "sample". Fail closed.
- **Duplicate submissions**: forbidden. Exactly one row per `(StudentId, AssignmentId)`, edited in place.
- **Retry rule** (implemented in Prompt 2, referenced here): score **below 85** → resubmit freely until she
  clears 85. **No attempt cap.**
- **Docker / Judge0 hosting**: out of scope, handled in the cloud.
- **Hebrew grammatical gender**: all user-facing copy addresses the student in the **feminine** form
  (`את`, `כתבת`, `נסי`) — this is a girls' school. Match the existing strings when adding new ones.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

This repo ships skills that encode its real conventions. Each step names the skills it requires. **Load them with
the Skill tool before editing, not after.** Do not hand-roll a pattern the repo already documents.

| Work | Skill |
|---|---|
| New Command/Query + Handler + FluentValidation | `backend-mediatr-query-handler-pattern` |
| New repository lookup / EF query | `backend-repository-query-pattern` |
| New controller action / route | `backend-controller-endpoint-pattern` |
| Entity ↔ DTO mapping | `backend-automapper-profile-pattern` |
| Anything under `client/src/app/pages/my/` | `client-student-area-pattern` |
| Copy, inline validation, confirm dialogs | `client-flow-fix-implementation-pattern` |
| List/table pages | `client-list-table-pattern` |
| Styling / tokens on a touched page | `client-design-token-rollout-pattern` |

Also obey [server/CLAUDE.md](../../server/CLAUDE.md) and [client/CLAUDE.md](../../client/CLAUDE.md): Clean
Architecture direction, CQRS, `IReadOnlyList` + `AsNoTracking` in repositories, `SaveChangesAsync` only through
`IUnitOfWork`, standalone Angular components, PrimeNG `MessageService`/`ConfirmationService`, `ApiClient` rather
than raw `HttpClient`.

---

## Step 1 — 🔴 Hide test answers from students

**Skills:** `backend-mediatr-query-handler-pattern`, `backend-automapper-profile-pattern`,
`client-flow-fix-implementation-pattern`

### The leak

`GetAssignmentByIdHandler` maps the assignment straight to `AssignmentResponseDto`, which carries
`List<TestCaseDto> Tests` — **including `Expected`**. `LessonsController.TeacherIdForSharedRead` is `null` for a
student, which is the *intended* shared-read path, so students call this endpoint legitimately and receive every
answer. `submit-code.component.ts:279` already reads `assignment.tests[0]` as an example.

Second path: `SubmissionResponseDto.TestResults` carries `Input`/`Expected`/`Actual` per test after grading.

The risk compounds with the retry rule from Prompt 2 — a student who sees `input 1234 → expected 10` can write
`if (n == 1234) return 10;` and resubmit.

### Model

Add `TestCase.IsSample` (bool, default **`false`** — fail closed).

- **Sample** → visible. A student needs at least one to understand the input format.
- **Hidden** → never transmitted, not even after grading.

Migration: every existing test becomes hidden. That is the safe default; teachers mark samples going forward.

### Fix both paths — **server-side**

| Path | Fix |
|---|---|
| Before submitting | `GetAssignmentByIdHandler` (and any other handler returning assignments) filters `Tests` to `IsSample` only **before** mapping to the DTO. The caller is already known: `TeacherIdForSharedRead is null` ⇒ student |
| After grading | The `GetSubmissionById` handler blanks `Input`/`Expected`/`Actual` on results belonging to hidden tests. `Passed` survives |

⚠️ **Filtering must happen server-side.** Hiding in an Angular template is worthless — the payload is already in
the browser and visible in DevTools.

### Client

- Assignment form: an "example test" checkbox per test row; the first row checked by default; a soft warning when
  no sample exists. ⚠️ [plan-gradingRequirementsEngine](plan-gradingRequirementsEngine.prompt.md) adds a
  **second per-test flag** (`IsCore` — core case vs edge case, which gates scoring). Build the test row to hold
  two checkboxes now, so that step is a data change rather than a layout rewrite. The two are orthogonal:
  *sample/hidden* controls **what the student sees**, *core/edge* controls **how it scores**
- Feedback panel: hidden rows render as `בדיקה 3 · מוסתרת ❌ נכשלה` with **no expand toggle**. The
  `עברו 3 מתוך 5` summary stays — it reveals nothing.

---

## Step 2 — 🔴 Block duplicate submissions

**Skills:** `backend-mediatr-query-handler-pattern`, `backend-repository-query-pattern`

**Rule: exactly one submission per `(StudentId, AssignmentId)`.** Further attempts edit that row (Prompt 2 adds
the retry rule and history).

`CreateSubmissionHandler` performs no existence check today, so a student who dislikes a grade of 40 simply
submits again and gets a second scored row. `DashboardComponent` then averages both.

1. `CreateSubmissionHandler` — look up an existing submission and throw `BusinessRuleException` with a message
   pointing at it.
2. Enforce in the database as well: a unique index on `(StudentId, AssignmentId)`, so two concurrent clicks
   cannot create two rows.
3. **Clean existing data before adding the index** — duplicates may already exist. Keep the latest by
   `SubmittedAt`, matching what `CompleteLessonHandler:31-33` already does when it picks a submission.

---

## Step 3 — 🔴 Hangfire durable storage

`Infrastructure/DependencyInjection.cs:64-68` registers Hangfire with `UseInMemoryStorage()`. A restart silently
drops every queued submission — they never grade and nobody is told.

Replace with SQLite-backed storage (the app already uses SQLite via the `"Default"` connection string).

While here, note but do **not** change: `AiWorker` deliberately swallows infrastructure exceptions
(`AiWorker.cs:96-117`), which is why Hangfire's automatic retry never fires. That is an intentional decision
recorded in the code comments.

---

## Step 4 — 🔴 Unstick the student

**Skills:** `client-student-area-pattern` (mandatory — this is the `/my` area),
`client-flow-fix-implementation-pattern`

**The server already supports this.** `UpdateSubmissionHandler.cs:51-63` permits editing for
`CompilationFailed` / `JudgeUnavailable` / `AiFailed`, resets the submission and re-enqueues it.
`PUT /api/students/{studentId}/submissions/{submissionId}` exists on `StudentsController` and is guarded by
`IsAllowedForStudent`. **Only the client is missing**, so a student who forgot a `;` is stranded.

1. **Route** in `app.routes.ts`, beside the existing `submissions/:submissionId`:
   ```ts
   { path: "submissions/:submissionId/edit", component: SubmitCodeComponent }
   ```
2. **Edit mode** in `submit-code.component.ts` — it only calls `.create()` today (line 372) and reads
   `lessonId`/`assignmentId` from the route. Detect a `submissionId`, load the existing submission into the form,
   and call `.update()` instead. **Reuse the existing editor, validation and file handling** — do not build a
   second screen.
3. **Button** in `my-feedback.component.ts` replacing the `failureNote` string (line 201, rendered for
   `CompilationFailed` at line 139 and `AiFailed` at line 153):
   ```
   ⚠  הקוד לא הצליח להתקמפל
   [explanation]
   [ תיקון והגשה מחדש ]
   ```
   Keep `judgeUnavailableNote` (line 202) as-is — an infrastructure failure is not the student's fault and there
   is nothing in her code to fix.

---

## Step 5 — Guardrails

| What | Where |
|---|---|
| **Assignment must be gradeable** | `CreateAssignmentCommandValidator` + `UpdateAssignmentCommandValidator` + the form. Today neither validates `Tests` at all, and `AiWorker.cs:159` turns `Total == 0` into a score of **0 for every student**. ⚠️ In Prompt 1 the rule is "at least one test"; **Prompt 2 relaxes it to "at least one test OR at least one structural requirement"** — a classes exercise ("write a `Student` class with a constructor and two properties") has no input and no output and is graded purely on structure. Write the validator so that relaxation is a one-line change |
| **Output normalization** | `Judge0CodeRunner` — clean **both sides** before comparing. Today `test.Expected` is base64'd verbatim, so a trailing space left in the teacher's form field or a CRLF pasted from Word fails the whole class. Normalize: `CRLF→LF`, **trim trailing whitespace on every line** (not just the end of the output), drop trailing blank lines. ⚠️ The per-line trim is essential for grid/matrix exercises — `Console.Write(m[i,j] + " ")` is the idiomatic way students print a matrix and leaves an invisible trailing space on every row, which would otherwise fail correct code |
| **Round the grade** | `Math.Round(..., 1)`. `66.66666666666666` is currently rendered raw in six screens |
| **Persist `Status.Description`** | into `TestCaseResult`. Judge0 already returns "Wrong Answer" vs "Runtime Error (SIGSEGV)" and it is discarded, so a wrong answer is indistinguishable from a crash |
| **Silent test wipe** | `PUT` with `tests: []` deletes every test case without warning — require explicit confirmation |

---

## Step 6 — 🔴 Code-runner bugs that fail blameless students

All four make a correct submission fail for reasons the student did not cause. They matter more once retries
exist (Prompt 2) — the student loops without ever learning why.

### 6.1 — `using` inside a class body (`GradingMode.Method`)

`BuildWrappedSource` (`Judge0CodeRunner.cs:226-248`) pastes the student's source **inside** a class:

```csharp
public static class StudentSolution
{
    {sourceCode}          // ← a `using System;` here is CS1529
}
```

Every C# lesson opens with `using System;`, so students write it by reflex and get a compile error unrelated to
the exercise. `MergeFiles` (lines 333-351) already hoists and de-duplicates `using` directives for the other two
paths — **route the Method path through it too** instead of pasting raw.

### 6.2 — Single-space `Split` on the input line

```csharp
var parts = (Console.ReadLine() ?? "").Split(' ');
```

A double space in the teacher's "Input" field yields `["3", "", "5"]`, and `int.Parse("")` throws — the student
sees a runtime failure caused by a typo in the assignment. Use
`Split(' ', StringSplitOptions.RemoveEmptyEntries)`, as `ExtractParameters` (line 263) already does.

### 6.3 — The student cannot print anything (`Method` mode)

The wrapper emits `Console.WriteLine(result)` itself. If the student also prints inside her method, stdout has
two lines and the test fails despite correct logic.

**This is a mode-selection problem, not a bug.** The system already supports both intents, and the teacher often
*does* ask students to print — so the fix is making the choice obvious rather than changing the runner:

| Teacher's intent | Correct mode | Who prints |
|---|---|---|
| "Write a method that returns the sum" | `Method` | the system |
| **"Read a number and print the sum"** | **`FullProgram`** (the default) | **the student** |

Rewrite the mode descriptions in `assignment-form.component.ts` (`gradingModeDescription`, lines 377-384) to say
this explicitly, and add the same one-line note to the student's submit screen for `Method` assignments:
*"בתרגיל מסוג זה המערכת מדפיסה את הערך המוחזר — אל תדפיסי בעצמך."*

Note for Prompt 2: "write a method **and** print it from `Main`" is expressible once structural requirements
exist — `FullProgram` mode plus a `MustUse Method` rule. Neither feature covers that case alone.

### 6.4 — Culture-dependent number formatting (**all modes**)

`Console.WriteLine(3.14)` and `double.Parse` use the container's current culture. On a decimal-comma locale the
program prints `3,14` while the expected value is `3.14`, failing every decimal test regardless of the code.

Force `CultureInfo.InvariantCulture` in the wrapper templates (`BuildWrappedSource`,
`BuildWrappedMultiFileSource`) — set it at the top of `Main` so both parsing and printing are stable no matter
where the container runs.

---

## Step 7 — Silent bugs

| Bug | Fix |
|---|---|
| **Student-average dialog is always empty** | `students-list.component.html:387-408` binds `studentSummary.average` and `.grades`, neither of which exists on `StudentGradesSummaryDto` (real shape: `{ studentId, studentName, courses[] }`, and the item field is `subject`, not `lessonName`). The DTO was regrouped by course and the template was never updated |
| **Bonus grades are rejected** | `CompleteLessonCommandValidator:15-16` enforces `InclusiveBetween(0, 100)` unconditionally, while `LessonResult.cs:22-27` allows up to 150 with bonus and the UI offers it. A teacher entering 130 gets a 400 |
| **`strictTemplates` is off** | `client/tsconfig.json` has no `angularCompilerOptions` block, which is **why the dialog bug compiled silently**. Enable it **last** and fix whatever it surfaces |
| **Dead `evaluatedBy`** | `submissions-list.component.ts:242` derives `s.feedback ? "AI" : "Manual"` — there is no manual grading, and the value is never rendered |

---

> **Note:** test-case authoring — letting the teacher verify her own tests, and having AI propose them — is a
> separate deliverable: [plan-testCaseAuthoring](plan-testCaseAuthoring.prompt.md). It should ship **before**
> the requirements engine makes tests all-or-nothing.

---

## Step 8 — Extract a new skill from the working code

**Skill:** `create-skill`

Do this **after** Step 1 works, following the repo's own convention — skills are extracted from working code, not
written up front (that is how `backend-excel-closedxml-pattern` came to exist).

Create **`backend-role-based-field-redaction`** covering the pattern Step 1 establishes:

- Deciding caller role at the controller boundary (`TeacherIdForSharedRead is null` ⇒ student) and passing it
  into the query, rather than inspecting `User` inside a handler
- Filtering/blanking sensitive fields **before** the AutoMapper call, never in the DTO or the template
- Why client-side hiding is not a control
- The two concrete sites: assignment `Tests` and submission `TestResults`

Mirror it into both `.github/skills/` and `.claude/skills/` per the root [CLAUDE.md](../../CLAUDE.md) — the two
copies must stay in sync.

---

## Verification

```bash
cd server && dotnet build SmartGrader.sln     # stop the running API first — it locks Infrastructure.dll
cd client && npx ng build
```

**Leak test — run as a student, against the network tab, not the screen:**

| # | Action | Expected |
|---|---|---|
| 1 | `GET /api/lessons/{id}/assignments/{id}` in DevTools | **Only** the sample test. No `Expected` for hidden ones |
| 2 | `GET` the submission after grading | Hidden results carry `passed` only; `input`/`expected`/`actual` blank |
| 3 | The same two calls **as a teacher** | Everything visible, unfiltered |
| 4 | Submit twice for one assignment | The second is rejected with a clear message |
| 5 | Submit code with a missing `;`, click the button, fix it | The editor opens **with the previous code**; the resubmit grades successfully |
| 6 | Restart the server with a submission queued | It survives and is graded |
| 7 | Create an assignment with zero tests | Blocked in the form and by the server validator |

**Code-runner regression tests (Step 6):**

| # | Setup | Expected |
|---|---|---|
| 8 | `Method` assignment; student's code opens with `using System;` | Compiles and grades normally |
| 9 | Test input typed with a double space: `3  5` | Parses as two arguments, no crash |
| 10 | A test whose expected value is `3.14`, correct code | Passes regardless of the container's locale |
| 11 | A matrix printed with `Console.Write(m[i,j] + " ")` — trailing space on every row | Passes; the per-line trim absorbs it |
| 12 | Expected value pasted from Word (CRLF), correct code | Passes |
