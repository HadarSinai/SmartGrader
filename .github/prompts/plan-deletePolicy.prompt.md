# Plan: Delete Policy — Protecting Student Work and Grades (מדיניות מחיקה)

## TL;DR

Today `DELETE /api/lessons/{id}` silently destroys every assignment, every student submission, and every final
grade under that lesson — and returns `204 No Content` with no warning. Same for deleting an assignment (kills its
submissions and leaves `LessonResult.FinalScore` stale) and deleting a student (kills her grade history and her
login account). The cause is five EF relationships left on the implicit `Cascade` default plus three delete
handlers with zero dependency checks. Fix: **block deletion whenever student work exists beneath it** — the same
block-if-not-empty pattern already used by `DeleteClassHandler` and `DeleteCourseHandler` — backed by explicit
`Restrict` FKs, Hebrew error messages that state the counts, and delete dialogs that say what would be lost.

**Guiding principle: grades and submissions are a permanent academic record. Deletion is permitted only when
nothing exists beneath it.**

## User decisions (already confirmed)

- **Blocking over soft-delete.** Full soft-delete (`IsDeleted` + `HasQueryFilter` on 6+ entities) was rejected as
  too large and regression-prone. Block-if-not-empty matches the existing codebase precedent.
- **No new "delete preview" endpoint.** The dependent counts are already in the list DTOs and already rendered on
  screen; reuse them for the dialog copy. The server guard stays the source of truth.
- **Teacher user deletion: document the policy only, build nothing.** There is no user-management screen today
  (no `UsersController`, no `users.service.ts`, no delete-user endpoint), so this is a rule for whoever adds one
  later — see Phase 5.
- **Out of scope:** `User.IsActive` / deactivation flow; bulk delete; any user-management UI.

## Key facts (from codebase research)

### The five implicit Cascades

All EF config lives in one file — `server/Infrastructure/Data/GradeSheetContext.cs` (105 lines,
`OnModelCreating`). The `Infrastructure/Data/Configurations/` folder exists but is **empty**; there are no
`*Configuration.cs` files.

| Relationship | Location | Effective behavior |
|---|---|---|
| `Assignment→Lesson` | `GradeSheetContext.cs:33-36` | **Cascade** (no explicit `OnDelete`) |
| `Submission→Student` | `GradeSheetContext.cs:28-31` | **Cascade** (no explicit `OnDelete`) |
| `Submission→Assignment` | `GradeSheetContext.cs:38-41` | **Cascade** (no explicit `OnDelete`) |
| `LessonResult→Student` | `GradeSheetContext.cs:47-50` | **Cascade** (no explicit `OnDelete`) |
| `LessonResult→Lesson` | **never configured at all** | **Cascade** (EF convention) |

Already correct and unchanged: `Student→User` = `SetNull` (`:61-65`), `Student→SchoolClass` = `Restrict`
(`:77-81`), `Lesson→Teacher` / `Lesson→Course` = `Restrict` (`:90-93`), `Course→Teacher` = `Restrict` (`:97`).
The comment at `:89` shows the author already understood the risk — it just never reached the child chain.

`LessonSchoolClasses` (M2M, `:84-87`) stays **Cascade** deliberately — it deletes only the association row.

### Handlers with no guard

- `DeleteLessonHandler.cs:15-25` — three lines: fetch, remove, save. Zero checks.
- `DeleteAssignmentHandler.cs:26-50` — checks ownership and lesson membership, but not submissions.
- `DeleteStudentHandler.cs:25-44` — no checks, and additionally hard-deletes the linked `User` (`:35-40`).

### Handlers that already do it right (the precedent to copy)

- `DeleteClassHandler.cs:26-27` → `BusinessRuleException("לא ניתן למחוק כיתה שיש בה תלמידים — יש להעביר לארכיון במקום")`
- `DeleteCourseHandler.cs:26-27` → `BusinessRuleException("לא ניתן למחוק קורס שיש בו שיעורים")`
- `DeleteSubmissionHandler.cs:79-85` → status guards on `ProcessingAi` / `Done`, **but the messages are English**.

### Other relevant state

- **No soft delete anywhere** — no `IsDeleted` / `DeletedAt` / `IsActive` fields, no `HasQueryFilter`. The only
  flag is `SchoolClass.IsArchived`, a business flag used by the year-rollover feature.
- **Year rollover already preserves history correctly**: `POST /api/classes/finish-year` →
  `SchoolClassRepository.ArchiveAllActiveAsync` sets `IsArchived = true` via `ExecuteUpdateAsync`. Nothing is
  deleted. This is the right tool for "end of year"; deletion is not.
- **No `Teacher` entity** — a teacher is a `User` with `Role = Teacher`. `IUserRepository.DeleteAsync` exists but
  is called from exactly one place (`DeleteStudentHandler`).
- `Log` has `int? UserId / LessonId / AssignmentId` with **no FK constraints** — orphans silently by design
  (audit trail). Leave as is.
- **Client double-toast bug**: every delete `.subscribe({ error })` discards the error and adds its own generic
  Hebrew toast *in addition to* the interceptor's toast. A blocked delete shows two toasts, one of them English
  titled `HTTP 400`.
- `api-error.interceptor.ts` has **no** status→Hebrew mapping: summary is literally `` `HTTP ${status}` `` and
  detail passes the server's raw `ProblemDetails.detail` through (English; on 500 it leaks the exception message).
- Counts already exist in the DTOs and are already rendered: `lesson.assignmentsCount`
  (`lessons-list.component.ts:199`), `assignment.submissionsCount` (`assignments-list.component.ts:139-140`),
  `student.submissionsCount` / `.lessonResultsCount` (`students-list.component.html:213-214`). **None** are used
  in a delete confirmation. The only exception — and the good template to copy — is
  `classes-list.component.ts:288-298` (block short-circuit) and `:337-348` (`confirmFinishYear`, which
  interpolates counts into Hebrew prose).

## Policy per entity

| Entity | Policy | Note |
|---|---|---|
| **Lesson** | Block if it has any assignments or lesson results | The main hole |
| **Assignment** | Block if it has any submissions | Cascade also leaves `LessonResult.FinalScore` stale |
| **Student** | Block if she has any submissions or lesson results | |
| **Submission** | Unchanged logic (`ProcessingAi` / `Done`) — translate messages to Hebrew | |
| **SchoolClass** | No change — already correct | |
| **Course** | No change — already correct | |
| **User (Teacher)** | Policy documented only, nothing built | See Phase 5 |

---

## Phase 1 — Close the hole (Backend, no schema change) ⚠️ do first

Highest urgency. Application layer only, no migration, shippable on its own.

### 1.1 New count methods

Per `backend-repository-query-pattern`: `AsNoTracking()`, `CancellationToken ct = default`, interface in
`Domain/Abstractions`, implementation in `Infrastructure/Repositories`. Use `CountAsync` — never load entities to
count them.

- `ILessonResultRepository` + `LessonResultRepository`:
  `CountByLessonIdAsync(int lessonId, ct)`, `CountByStudentIdAsync(int studentId, ct)`
- `ISubmissionRepository` + `SubmissionRepository`:
  `CountByAssignmentIdAsync(int assignmentId, ct)`, `CountByStudentIdAsync(int studentId, ct)`
- `IAssignmentRepository` + `AssignmentRepository`:
  `CountByLessonIdAsync(int lessonId, ct)`

```csharp
public Task<int> CountByLessonIdAsync(int lessonId, CancellationToken ct = default)
    => _context.LessonResults.AsNoTracking().CountAsync(x => x.LessonId == lessonId, ct);
```

### 1.2 Guards in the handlers

Messages are **Hebrew, gender-neutral**, and state the reason, the counts, and the alternative. Thrown as
`BusinessRuleException` → 400 with `detail` = the message (already wired at `GlobalExceptionMiddleware.cs:91`).

**`DeleteLessonHandler.cs`** — inject `IAssignmentRepository` + `ILessonResultRepository`, after
`LessonAccess.GetOwnedOrThrowAsync`:

```csharp
var assignmentsCount = await _assignmentRepository.CountByLessonIdAsync(request.Id, cancellationToken);
var resultsCount     = await _lessonResultRepository.CountByLessonIdAsync(request.Id, cancellationToken);

if (assignmentsCount > 0 || resultsCount > 0)
    throw new BusinessRuleException(
        $"לא ניתן למחוק שיעור שיש בו תרגילים או ציונים ({assignmentsCount} תרגילים, {resultsCount} ציונים). " +
        "עבודת התלמידות והציונים נשמרים כרשומה אקדמית.");
```

**`DeleteAssignmentHandler.cs`** — inject `ISubmissionRepository`, after the existing step 3:

```csharp
var submissionsCount = await _submissionRepository.CountByAssignmentIdAsync(request.AssignmentId, cancellationToken);
if (submissionsCount > 0)
    throw new BusinessRuleException(
        $"לא ניתן למחוק תרגיל שיש עליו {submissionsCount} הגשות. " +
        "מחיקת התרגיל תמחק את עבודת התלמידות ואת הציונים שחושבו על בסיסה.");
```

**`DeleteStudentHandler.cs`** — inject `ISubmissionRepository` + `ILessonResultRepository`, before the `User`
deletion:

```csharp
var submissionsCount   = await _submissionRepository.CountByStudentIdAsync(request.Id, cancellationToken);
var lessonResultsCount = await _lessonResultRepository.CountByStudentIdAsync(request.Id, cancellationToken);

if (submissionsCount > 0 || lessonResultsCount > 0)
    throw new BusinessRuleException(
        $"לא ניתן למחוק תלמיד/ה עם {submissionsCount} הגשות ו-{lessonResultsCount} ציונים. " +
        "הרשומה האקדמית נשמרת — אפשר להעביר את הכיתה לארכיון בסיום שנה.");
```

The `User` deletion stays as-is — it now only runs for an empty student, so no grades can be lost. Add a `// TODO`
noting the deactivate-instead-of-delete direction.

**`DeleteSubmissionHandler.cs`** — translation only, logic unchanged:

```csharp
// ProcessingAi
"לא ניתן למחוק הגשה בזמן שהבדיקה האוטומטית פועלת. יש להמתין לסיום הבדיקה."
// Done
"לא ניתן למחוק הגשה שכבר נבדקה וקיבלה ציון — זו רשומה אקדמית קבועה."
```

**`DeleteClassHandler` / `DeleteCourseHandler`** — no change.

---

## Phase 2 — Fix the error messages (Client only)

Without this the Hebrew messages from Phase 1 do not reach the teacher properly. Can run in parallel with Phase 3.

### 2.1 Hebrew status→message map in the interceptor

`client/src/app/core/http/api-error.interceptor.ts`:

```ts
const SUMMARY_HE: Record<number, string> = {
  400: "הפעולה נחסמה",
  403: "אין הרשאה לפעולה זו",
  404: "הפריט לא נמצא",
  409: "קיים כבר פריט זהה",
  500: "שגיאת שרת",
  0:   "אין חיבור לשרת",
};
```

- `summary = SUMMARY_HE[status] ?? "שגיאה"` — never `HTTP 400` again.
- `detail`: use `err.error?.detail` **only if it contains Hebrew** (`/[\u0590-\u05FF]/.test(d)`), otherwise fall
  back to a Hebrew string per status. This stops English text and leaked exception messages from reaching the
  teacher.
- **Keep all existing skip conditions unchanged** (`:12-27`): auth URLs, 401, and 404 on
  `/api/lesson-results/{n}/{n}`.

### 2.2 Remove the double-toast

Delete the `error` branch that adds a toast, in all six files. After 2.1 the interceptor already shows the
specific Hebrew reason — strictly better than "מחיקת השיעור נכשלה".

- `lessons-list.component.ts:526-532`
- `students-list.component.ts:218-224`
- `assignments-list.component.ts:424-430`
- `submissions-list.component.ts:229-235`
- `classes-list.component.ts:320-326`
- `logs-list.component.ts:329-336`

The success toast (`next:`) stays. If a component needs to reset a loading flag, use
`error: () => { this.deleting = false; }` with **no** `messageService.add`.

---

## Phase 3 — Defense in depth (DB constraints)

The Phase 1 guards mean no legitimate request ever reaches these constraints. They exist so a future handler that
forgets a guard fails **loudly** instead of silently deleting.

### 3.1 `GradeSheetContext.cs` — every OnDelete explicit

```csharp
// Submission → Student : Restrict — הגשות הן רשומה אקדמית, לא נמחקות עם התלמידה
modelBuilder.Entity<Student>()
    .HasMany(s => s.Submissions).WithOne(s => s.Student)
    .HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);

// Assignment → Lesson : Restrict — מחיקת שיעור לא תמחק תרגילים (ודרכם הגשות)
modelBuilder.Entity<Lesson>()
    .HasMany(l => l.Assignments).WithOne(a => a.Lesson)
    .HasForeignKey(a => a.LessonId).OnDelete(DeleteBehavior.Restrict);

// Submission → Assignment : Restrict
modelBuilder.Entity<Assignment>()
    .HasMany(a => a.Submissions).WithOne(s => s.Assignment)
    .HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Restrict);

// LessonResult → Student : Restrict
modelBuilder.Entity<Student>()
    .HasMany(s => s.LessonResults).WithOne(r => r.Student)
    .HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Restrict);

// LessonResult → Lesson : Restrict — עד היום לא הוגדר כלל
modelBuilder.Entity<Lesson>()
    .HasMany<LessonResult>().WithOne(r => r.Lesson)
    .HasForeignKey(r => r.LessonId).OnDelete(DeleteBehavior.Restrict);
```

**Note:** `Lesson` has no `ICollection<LessonResult>` navigation (`Lesson.cs:15-16`), so the no-navigation
overload `HasMany<LessonResult>()` is required. Do **not** add a navigation to `Lesson` just for this.

Add a Hebrew comment block above `OnModelCreating` documenting the policy so the reasoning survives.

### 3.2 Migration — required

```bash
dotnet ef migrations add EnforceDeleteRestrictOnAcademicRecords -p Infrastructure -s Api
```

**SQLite warning:** SQLite does not support `ALTER TABLE ... DROP CONSTRAINT`. EF implements an FK-behavior change
by **rebuilding the table** (`ef_temp_*` → copy → drop → rename). Expect rebuilds of `Assignments`, `Submissions`,
`LessonResults`. Therefore:

1. **Back up `smartgrader.db`** (file copy) before applying.
2. **Read the generated migration before running it** — confirm every column survives, including `TestsJson`,
   `FeedbackJson`, `SourceFiles`. (Per `server/CLAUDE.md`, never hand-edit a migration — but do read it.)
3. **Verify there are no orphan rows first**, otherwise the new FK will reject the rebuild:

```sql
SELECT COUNT(*) FROM Submissions s LEFT JOIN Assignments a ON s.AssignmentId=a.Id WHERE a.Id IS NULL;
SELECT COUNT(*) FROM LessonResults r LEFT JOIN Lessons l ON r.LessonId=l.Id WHERE l.Id IS NULL;
```

Both must return 0.

### 3.3 Fix the `AsNoTracking` + `Remove` inconsistency

`CourseRepository.cs:32-38` has an explicit comment that it **deliberately omits** `AsNoTracking()`, because the
entity is loaded for read-then-delete. `SchoolClassRepository.GetByIdAsync:32-38` did not follow suit — it uses
`AsNoTracking()` and the entity is then passed to `Remove()`. Remove `.AsNoTracking()` there and add the same
comment. Verify the same in `StudentRepository.GetByIdAsync` and `SubmissionRepository.GetByIdAsync`.

---

## Phase 4 — Dialogs that state the consequences (Client)

### 4.1 No new endpoint — reuse the counts already present

A `GET /api/lessons/{id}/delete-preview` would add a controller action + query + handler + DTO + validator +
service method + round-trip, to display numbers the client **already holds in memory**.

**The one genuine gap:** `lessonResultsCount` is missing from `LessonResponseDto`. Add it server-side (computed
alongside `AssignmentsCount` in `LessonProfile` / the Get handler) and in `client/src/app/models/lesson.model.ts`.

**The server remains the source of truth** — client counts only phrase the dialog; if stale, the server still
returns 400 with the accurate message. Exactly the pattern already used for classes.

### 4.2 Two dialog states

Keep `ConfirmationService`, the header, and the labels. Template to copy: `classes-list.component.ts:288-298`
(block short-circuit) and `:337-348` (counts in Hebrew prose).

**(a) Blocked — short-circuit before opening the dialog:**

```ts
confirmDelete(lesson: LessonResponseDto): void {
  const a = lesson.assignmentsCount ?? 0;
  const r = lesson.lessonResultsCount ?? 0;
  if (a > 0 || r > 0) {
    this.messageService.add({
      severity: "warn",
      summary: "לא ניתן למחוק",
      detail:
        `לשיעור "${lesson.name}" יש ${a} תרגילים ו-${r} ציונים. ` +
        `עבודת התלמידות והציונים נשמרים כרשומה אקדמית.`,
    });
    return;
  }
  this.confirmationService.confirm({
    message: `האם למחוק את השיעור "${lesson.name}"? לשיעור אין תרגילים או ציונים. לא ניתן לשחזר פעולה זו.`,
    header: "אישור מחיקה",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "מחיקה",
    rejectLabel: "ביטול",
    accept: () => this.deleteLesson(lesson.id),
  });
}
```

**(b) Empty — the dialog now explicitly confirms there is nothing to lose** (instead of staying silent):

- **Assignment:** empty → `לתרגיל אין הגשות.` / blocked → `לתרגיל "${title}" יש ${n} הגשות…`
- **Student:** empty → `לתלמיד/ה אין הגשות או ציונים.` / blocked → `ל${fullName} יש ${s} הגשות ו-${r} ציונים…`
- **Submission** (status-based, no counts): `Done` → `לא ניתן למחוק הגשה שכבר נבדקה וקיבלה ציון.`;
  `ProcessingAi` → `ההגשה נבדקת כעת — יש להמתין לסיום.`; otherwise → normal dialog.

### 4.3 Copy rules

Hebrew only, gender-neutral (`תלמיד/ה`, `נמחק/ה`), no English. Numbers inside RTL sentences (`${a} תרגילים` reads
correctly; avoid `(3)` at the start of a string).

### 4.4 Disable the delete action up front

When counts > 0, disable the delete item in the ⋯ menu with a `pTooltip` explaining why in Hebrew, so the block is
visible **before** the click. The warn toast stays as a safety net.

---

## Phase 5 — Teacher user deletion: policy only, nothing built

Same principle as the student, applied to a teacher `User`.

**Current state, verified:** no `UsersController`, no `users.service.ts`, no `DELETE /api/users/{id}`.
`IUserRepository.DeleteAsync` is called from exactly one place — `DeleteStudentHandler`. `Lesson→Teacher` and
`Course→Teacher` are already `Restrict`, so even a direct DB attempt fails.

**Decision: build nothing now.** Record the rule so whoever adds user management later implements it correctly:

> **Deleting a `User` with `Role = Teacher` is permitted only if she has zero lessons and zero courses.** If she
> has any, block with `BusinessRuleException` and a Hebrew message including the counts, e.g.
> `לא ניתן למחוק מורה שיש לה 4 שיעורים ו-2 קורסים. השיעורים והציונים של התלמידות נשמרים כרשומה אקדמית.`
>
> Implementation when the time comes: `CountByTeacherIdAsync` on `ILessonRepository` and `ICourseRepository`, a
> `DeleteUserHandler` guard mirroring `DeleteStudentHandler`, and an Admin-only endpoint. Prefer **deactivation**
> (an `IsActive` flag blocking login) over deletion for retiring a teacher who has history — that preserves her
> lessons and her students' grades intact.

No files change in this phase.

---

## Key Files

**Server**

- `server/Infrastructure/Data/GradeSheetContext.cs` — all `OnDelete` settings
- `server/Application/UseCases/Lessons/DeleteLesson/DeleteLessonHandler.cs`
- `server/Application/UseCases/Assignments/DeleteAssignment/DeleteAssignmentHandler.cs`
- `server/Application/UseCases/Student/DeleteStudent/DeleteStudentHandler.cs`
- `server/Application/UseCases/Submission/DeleteSubmission/DeleteSubmissionHandler.cs` — translation only
- `server/Domain/Abstractions/I{Assignment,Submission,LessonResult}Repository.cs` + implementations in
  `server/Infrastructure/Repositories/`

**Client**

- `client/src/app/core/http/api-error.interceptor.ts`
- The six `*-list.component.ts` files (lessons, students, assignments, submissions, classes, logs) — remove the
  duplicate error toast + rewrite `confirmDelete`
- `client/src/app/models/lesson.model.ts` — add `lessonResultsCount: number`

---

## Verification

### Build

```bash
cd "d:/פרויקט עולמי הדר/server" && dotnet build SmartGrader.sln
cd "d:/פרויקט עולמי הדר/client" && npm run build
```

### Migration (Phase 3 only)

```bash
# back up smartgrader.db first
dotnet ef migrations script <prev> EnforceDeleteRestrictOnAcademicRecords -p Infrastructure -s Api   # read before applying
dotnet ef database update -p Infrastructure -s Api
```

Confirm the constraint landed:

```sql
PRAGMA foreign_key_list(Assignments);    -- on_delete should be NO ACTION/RESTRICT, not CASCADE
PRAGMA foreign_key_list(Submissions);
PRAGMA foreign_key_list(LessonResults);
```

### Manual API checks (teacher token)

```bash
# 1. Lesson with assignments → 400 + Hebrew detail with counts
curl -i -X DELETE http://localhost:5000/api/lessons/<lessonWithAssignments> -H "Authorization: Bearer $TOKEN"
# 2. Empty lesson → 204
curl -i -X DELETE http://localhost:5000/api/lessons/<emptyLesson> -H "Authorization: Bearer $TOKEN"
# 3. Assignment with submissions → 400 Hebrew
curl -i -X DELETE http://localhost:5000/api/lessons/<lid>/assignments/<aidWithSubmissions> -H "Authorization: Bearer $TOKEN"
# 4. Student with grades → 400 Hebrew
curl -i -X DELETE http://localhost:5000/api/students/<studentWithGrades> -H "Authorization: Bearer $TOKEN"
# 5. Graded submission → 400, message now in Hebrew
curl -i -X DELETE http://localhost:5000/api/students/<sid>/submissions/<doneSubmissionId> -H "Authorization: Bearer $TOKEN"
# 6. Regression: class and course still blocked as before
curl -i -X DELETE http://localhost:5000/api/classes/<classWithStudents> -H "Authorization: Bearer $TOKEN"
curl -i -X DELETE http://localhost:5000/api/courses/<courseWithLessons> -H "Authorization: Bearer $TOKEN"
```

### UI checks (Hebrew, RTL)

1. Lessons list → ⋯ on a lesson with assignments: delete item is disabled with a Hebrew tooltip; clicking shows
   **exactly one** toast with the counts, no `HTTP 400`.
2. Lessons list → ⋯ on an empty lesson: dialog reads `לשיעור אין תרגילים או ציונים` → accept → one success toast,
   row disappears.
3. Same two paths for Assignments and Students.
4. Delete a graded submission → one Hebrew toast, no English.
5. Stop the API server and attempt a delete → `אין חיבור לשרת` (not `HTTP 0`).
6. `סיום שנה` still works; classes archive; students and grades remain viewable.
