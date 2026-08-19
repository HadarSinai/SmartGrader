# Plan: Authorization & Data-Safety Fixes (Prompt 0 — do this first)

## TL;DR

An audit ahead of the grading overhaul found that **ownership checks are missing across most of the API**: a
student can read every lesson and assignment in the school (including all test cases and expected answers) and
submit to assignments she was never given; any teacher can read, edit and delete any other teacher's submissions
and write final grades for their students; and deleting a lesson silently cascade-deletes every student's code,
feedback and grades behind a confirmation dialog that never mentions it.

None of this has caused harm — **the system is still in development, with no real students** — but all of it must
close before a class is onboarded, and before the two grading prompts build on top of it.

This file is deliberately small and independent. Ship it first.

**Then:** [plan-gradingSecurityHardening](plan-gradingSecurityHardening.prompt.md) →
[plan-gradingRequirementsEngine](plan-gradingRequirementsEngine.prompt.md).

> ✅ **Shipped 2026-08-19.** All 7 steps implemented and verified against the running API
> (26/26 checks). See [What was implemented](#what-was-implemented) at the bottom for the
> per-step record, the three places the implementation deviated from this plan, and what was
> deliberately left alone.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| Handler changes, validators | `backend-mediatr-query-handler-pattern` |
| Repository queries and scoping | `backend-repository-query-pattern` |
| Controller actions, route auth | `backend-controller-endpoint-pattern` |
| Confirm dialogs and copy | `client-flow-fix-implementation-pattern` |

Hebrew copy addresses the student in the **feminine** form — this is a girls' school. Follow
[server/CLAUDE.md](../../server/CLAUDE.md) and [client/CLAUDE.md](../../client/CLAUDE.md).

---

## Step 1 — 🔴 A student can read every lesson and assignment in the school

`LessonAccess.GetOwnedOrThrowAsync` only enforces ownership when a teacher id is present
(`LessonAccess.cs:23`), and `LessonsController.TeacherIdForSharedRead` (lines 38-39) is `null` for a student **by
design**, so the check never runs for her:

```csharp
private int? TeacherIdForSharedRead =>
    (User.IsInRole("Teacher") || User.IsInRole("Admin")) ? OwnerScopeTeacherId : null;
```

Consequently `GET /api/lessons/{id}` (66-73), `GET /api/lessons/{id}/assignments` (113-122) and
`.../assignments/{id}` (125-137) are **unscoped for students**. Any student can enumerate every lesson and every
assignment in the database — including `Tests` with expected outputs. Class filtering exists only on the *list*
endpoint (`GetLessonsHandler.cs:34-42`).

**Fix:** carry the student's identity into these queries the way the list endpoint already does, and reject a
lesson not assigned to her class with `NotFoundException` (not `Forbid` — do not confirm the lesson exists).
Derive `studentId` from the token claim only, never from the request.

## Step 2 — 🔴 A student can submit to an assignment she was never given

`CreateSubmissionHandler.cs:46-57` validates only that the student and the assignment exist. There is no check
that the assignment's lesson is assigned to her class, so combined with Step 1 she can create graded submissions
against any assignment in the system.

**Fix:** validate lesson↔class membership in the handler, reusing the scoping helper from Step 1.

## Step 3 — 🔴 Any teacher can reach any other teacher's submissions

`ApiControllerBase.IsAllowedForStudent` (lines 33-40, duplicated at `StudentsController.cs:39-46`) returns `true`
for **any** teacher, and nothing downstream re-scopes:

| Handler | Line | Gap |
|---|---|---|
| `GetSubmissionsHandler` | 30-32 | `GetByStudentIdAsync` with no teacher filter |
| `GetSubmissionByIdHandler` | 29-40 | checks only `submission.StudentId == request.StudentId` |
| `UpdateSubmissionHandler` | 37-48 | same |
| `DeleteSubmissionHandler` | 66-88 | same |

Teacher B can read the source code, AI feedback and scores of Teacher A's students, and delete or resubmit their
work.

**Fix:** scope every submission query by lesson ownership. The correct idiom already exists in the codebase —
`SubmissionRepository.GetRecentGradedAsync:62-63` filters on
`s.Assignment.Lesson.TeacherId == teacherId` — it is simply unused outside notifications. Apply it consistently.

## Step 4 — 🔴 Any teacher can write final grades for any student

`POST /api/lesson-results/complete` (`LessonResultController.cs:62-73`) is guarded only by
`[Authorize(Roles = "Teacher,Admin")]`. `CompleteLessonCommand` (lines 11-16) carries **no teacher id at all**,
and `CompleteLessonHandler` never checks one — so any authenticated teacher can set the final grade of any
student on any lesson.

Note that `Export` (line 55) and `ExportPeriodReport` (line 83) in the same controller **do** pass
`OwnerScopeTeacherId`. This one slipped through because the parameter does not exist on the command, which is
precisely the failure mode `ApiControllerBase.cs:20-23` warns about.

**Fix:** add `TeacherId` to the command, pass `OwnerScopeTeacherId` from the controller, and call
`LessonAccess.GetOwnedOrThrowAsync` in the handler. Audit every other command in `LessonResults` for the same
omission.

## Step 5 — 🔴 Deleting a lesson destroys every grade, silently

The model snapshot confirms the chain is on EF's default cascade:

| Relationship | Line | Behaviour |
|---|---|---|
| `Assignment → Lesson` | 368-372 | `Cascade` |
| `Submission → Assignment` | 446-450 | `Cascade` |
| `Submission → Student` | 451ff | `Cascade` |
| `LessonResult → Lesson` | 409-413 | `Cascade` |
| `LessonResult → Student` | 415-419 | `Cascade` |

So `DeleteLessonHandler.cs:15-25` — a bare `DeleteAsync` + `SaveChanges` — wipes the lesson's assignments, every
submission (source, feedback, scores) and every final grade. Deleting a student does the same plus hard-deletes
her login row (`DeleteStudentHandler.cs:29-43`).

The guard rail that does exist is on the leaf only: `DeleteSubmissionHandler.cs:79-85` refuses to delete a
`ProcessingAi` or `Done` submission — **trivially bypassed by deleting the parent instead**, since the cascade
never consults it.

The confirmation text says nothing about any of it:

> `lessons-list.component.ts:507` — *"האם למחוק את השיעור "..."? לא ניתן לשחזר פעולה זו."*

The intent was clearly the opposite. `GradeSheetContext.cs:89` documents `Restrict` on `Lesson→Teacher`
explicitly *because* "cascade היה מוחק Assignments → Submissions → עבודת תלמידים" — but the
Lesson→Assignment→Submission chain itself was left on cascade.

**Fix, in order:**

1. Change `Assignment → Lesson` and `Submission → Assignment` to `Restrict`
2. Block deletion in the handlers when graded work exists, with a message that says what is blocking
3. If deletion must remain possible, require a **second explicit confirmation naming the consequence** —
   *"לשיעור זה יש 23 הגשות ו-12 ציונים סופיים. מחיקה תמחק גם אותם לצמיתות."*
4. Prefer archiving over deleting where the domain already supports it (`SchoolClass.IsArchived` exists)

⚠️ `AiWorker.cs:41-42` currently no-ops when a queued submission has vanished. Once deletes are restricted that
path changes — verify a queued job for a now-undeletable submission still behaves.

## Step 6 — A `Student` login with no linked student record breaks silently

`LoginHandler.cs:39-44` emits `studentId = student?.Id`, which is **null** when a `User` has `Role = Student` but
no linked `Student` row, and `JwtTokenGenerator.cs:34-35` then omits the claim entirely. `studentGuard`
(`auth.guards.ts:35-42`) checks only the role, so such a user passes into `/my/lessons`.

Every page then bails before setting `loading` — `my-lessons-list.component.ts:176`,
`my-assignments-list.component.ts:205`, `my-grades.component.ts:387`, `my-feedback.component.ts:233` — rendering
blank screens, and `submit-code.component.ts:367` makes the submit button a **dead no-op**: no toast, no error,
nothing.

There is also a redirect loop: `auth.service.ts:82-87` returns `["/"]` for a student without a `studentId`, and
`/` is `teacherGuard`-protected (`app.routes.ts:43`), which redirects back to `homeRoute()`.

**Fix:** `studentGuard` requires both the role **and** the claim; a student without one is signed out with a
clear message rather than dropped into blank pages. Prevent the state at the source by refusing to create a
`Student` login without a linked student record. Give `403` a real message in `api-error.interceptor.ts:36-41` —
only `401` is handled today.

## Step 7 — Small hardening

| What | Where |
|---|---|
| **Unbounded notification limit** | `GetRecentGradedSubmissionsQueryValidator.cs:10` enforces only `Limit > 0`; `?limit=100000` is accepted. Cap it |
| **Exports leak the whole school** | `ExportLessonResultsHandler.cs:43` and `ExportGradesPeriodReportHandler.cs:42` call `GetAllAsync(ct)`, which defaults to `includeArchived: true` (`StudentRepository.cs:24-25`). Every teacher's export lists every student in the school, archived years included. Scope the roster to the lesson's classes and exclude archived students |
| **N+1 in the lesson export** | `ExportLessonResultsHandler.cs:61` issues one `GetByStudentAndLessonAsync` per student, each with two `Include`s |

---

## Verification

```bash
cd server && dotnet build SmartGrader.sln     # stop the running API first — it locks Infrastructure.dll
cd client && npx ng build
```

Test **against the API directly**, with tokens for two teachers and two students in different classes:

| # | As | Action | Expected |
|---|---|---|---|
| 1 | Student in class A | `GET /api/lessons/{id}` for a class-B lesson | 404 |
| 2 | Student in class A | `GET /api/lessons/{id}/assignments/{id}` for a class-B assignment | 404 |
| 3 | Student in class A | `POST` a submission to a class-B assignment | Rejected |
| 4 | Teacher B | `GET /api/students/{id}/submissions` for Teacher A's student | 404 / 403 |
| 5 | Teacher B | `DELETE` one of Teacher A's submissions | Rejected |
| 6 | Teacher B | `POST /api/lesson-results/complete` on Teacher A's lesson | Rejected |
| 7 | Teacher A | All of the above on her **own** data | Works unchanged |
| 8 | Teacher A | Delete a lesson that has submissions | Blocked, or a second confirmation naming the counts |
| 9 | Student login with no linked record | Log in | Clear message; no blank pages, no redirect loop |
| 10 | Any teacher | Export lesson results | Only students in that lesson's classes; no archived students |

---

## What was implemented

Shipped 2026-08-19. `dotnet build` and `npx ng build` both clean. The verification table above was
run as a script against the live API with a real fixture (two teachers, two classes, two students,
lessons owned by teacher A) — **26/26 checks passed**, including the "teacher A's own data still
works" row, which is the one that catches over-scoping.

### Step 1 — student scoping on lesson/assignment reads

New `LessonAccess.GetAccessibleOrThrowAsync(lessonRepo, studentRepo, lessonId, teacherId, studentId, ct)`
sits alongside the existing `GetOwnedOrThrowAsync`. When `teacherId` is null because the caller is a
student, `studentId` carries the scope instead, and the lesson must be assigned to her class —
`NotFoundException`, not `Forbid`. The class is read from the DB by student id, never from the request.

- `GetLessonByIdQuery` / `GetAssignmentsQuery` / `GetAssignmentByIdQuery` each gained `int? StudentId`
- `LessonsController` resolves the scope through the new `ApiControllerBase.TryResolveSharedReadScope`

`TryResolveSharedReadScope` returns **false** for a caller who is neither teacher/admin nor holds a
`studentId` claim, and the controller answers 403. Without that branch such a caller would have fallen
through with both ids null — i.e. unscoped — which is the exact shape of the original bug.

### Step 2 — submission must belong to a lesson given to her class

`CreateSubmissionHandler` now loads the assignment's lesson and checks class membership via
`LessonAccess.IsAssignedToClass`. Throws `NotFoundException(nameof(Assignment), …)` so it does not
confirm the assignment exists. Applies to teachers submitting on a student's behalf too.

### Step 3 — submissions scoped by lesson ownership

`ISubmissionRepository.GetByIdAsync` and `GetByStudentIdAsync` now take `int? teacherId`, filtering on
`s.Assignment.Lesson.TeacherId` — the idiom that already existed in `GetRecentGradedAsync` and was
unused elsewhere. Following the `ILessonRepository` precedent there is **no overload without the
parameter**, so a future call site cannot silently omit it.

`GetSubmissionsQuery`, `GetSubmissionByIdQuery`, `UpdateSubmissionCommand` and
`DeleteSubmissionCommand` all carry `TeacherId`. `StudentsController` now extends `ApiControllerBase`
and its duplicated `IsAllowedForStudent` was deleted. `AiWorker` passes `teacherId: null` explicitly —
it is a system caller, not a user.

### Step 4 — final grades scoped to the lesson owner

`CompleteLessonCommand` gained `TeacherId`, placed **before** `HasBonus` because a parameter without a
default cannot follow one with a default. `CompleteLessonHandler` calls `LessonAccess.GetOwnedOrThrowAsync`.

The audit of the rest of `LessonResults` found one more gap: `GetLessonResultQuery` had no `TeacherId`,
and the ownership check was being done **in the controller** instead. Both fixed — the check moved into
the handler, and `LessonResultController` no longer injects `ILessonRepository` at all.
`ExportLessonResults`, `ExportGradesPeriodReport` and `GetStudentGradesSummary` were already correct.

### Step 5 — cascade deletes stopped

Migration `20260819180342_RestrictGradedWorkCascades` (applied). `Assignment→Lesson`,
`Submission→Assignment` and `LessonResult→Lesson` are now `Restrict`. `LessonResult→Lesson` was not in
the plan's list but had to be included: without it, deleting a lesson with no assignments still wiped
its final grades.

Guards in the handlers, each naming what is blocking:

| Handler | Blocks when |
|---|---|
| `DeleteLessonHandler` | any submission or lesson result under the lesson |
| `DeleteAssignmentHandler` | any submission under the assignment |
| `DeleteStudentHandler` | any submission or lesson result for the student; suggests archiving instead |

An empty lesson is still deletable, and deletes its assignments **explicitly in the handler** — the
`Restrict` edge means no cascade happens by itself, which is the point.

New repository counts: `ISubmissionRepository.CountByLessonIdAsync` / `CountByAssignmentIdAsync` /
`CountByStudentIdAsync`, and `ILessonResultRepository.CountByLessonIdAsync` / `CountByStudentIdAsync`.

On the `AiWorker.cs:41-42` warning: the no-op path was kept and commented. It is now nearly
unreachable — a `PendingAi`/`ProcessingAi` submission cannot be deleted, and the parent-delete bypass
is closed — but it stays as a guard.

### Step 6 — Student login with no linked record

Blocked in three layers:

1. `LoginHandler` refuses to issue a token at all (`BusinessRuleException` with Hebrew copy)
2. `studentGuard` requires the **claim**, not just the role; signs out with a message
3. `submit-code.component.ts` no longer returns silently — the dead button now explains itself

`AuthService.homeRoute()` returns `/login` (not `/`) for a student without a `studentId`. That single
line is what closed the redirect loop: `/` is `teacherGuard`-protected and bounced straight back.
`api-error.interceptor.ts` gained a 403 branch — 403 arrives with an empty body, so the generic branch
was showing the raw `Http failure response for …` string.

### Step 7 — hardening

- `GetRecentGradedSubmissionsQueryValidator`: `InclusiveBetween(1, 100)` instead of `GreaterThan(0)`
- Both exports moved off `GetAllAsync()` to a new
  `IStudentRepository.GetByClassIdsAsync(classIds, includeArchived: false, ct)`, scoped to the lesson's
  classes. `LessonRepository.GetByDateRangeAsync` gained `.Include(l => l.Classes)` to make that possible
  for the period report.
- N+1 removed: new `ISubmissionRepository.GetByLessonIdAsync` fetches the whole lesson's submissions in
  one query, replacing one `GetByStudentAndLessonAsync` (with two `Include`s) per student

---

### Where the implementation deviated from this plan

1. **Step 5 chose blocking over a second confirmation.** The plan offered either. Blocking is what
   shipped, so there is no "type the lesson name to confirm" dialog. The client confirmation copy was
   still corrected to say what else gets deleted (*"כל התרגילים שלו יימחקו גם הם"*), and the server's
   message names the counts when a delete is refused.
2. **Step 6's "prevent the state at the source" produced no code.** Both account-creation paths were
   audited and are already correct: `CreateStudentAccountHandler` writes `User` + `Student` in one
   `SaveChanges`, and `CreateAccountForStudentHandler` links to an existing student or fails. **No API
   route can create the orphan state** — it can only arrive from a manual DB edit or pre-existing data.
   The enforcement therefore sits at login, where the state actually surfaces.
3. **`LessonResult→Lesson` was added to the `Restrict` list** (see Step 5 above).

### Found while verifying

The first verification run failed one check with a 500: deleting an empty lesson threw
*"The instance of entity type 'Assignment' cannot be tracked…"*. `DeleteLessonHandler` was pulling the
assignments through `GetByLessonIdAsync`, which is `AsNoTracking`, while the tracked `Lesson` already
held the same rows via its `Assignments` navigation. Fixed by deleting through `lesson.Assignments`.
Worth remembering for any other handler that deletes children of an already-loaded aggregate.

### Deliberately left alone

- `ExportStudentsHandler` still calls `GetAllAsync()` — it is the teacher-facing roster screen, and the
  plan scoped only the two lesson-results exports.
- `CompleteLessonHandler` checks lesson **ownership** but does not require the student to be in the
  lesson's class. Adding that would break grading for a student who changed classes mid-year; flagged
  here rather than decided silently.
- Students remain shared across teachers by design (`GET /api/students` is unscoped for teachers) —
  the plan did not ask to change that, and `plan-gradingSecurityHardening` may want to revisit it.
