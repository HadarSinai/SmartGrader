# Plan: Per-Teacher Lesson Ownership + Courses + Per-Course Averages

## TL;DR

Today there is **no concept of "a teacher's own lessons"**. `Lesson.TeacherName` is a free-text string typed into
a form — not an FK, not linked to `User` (grep for `TeacherId`/`OwnerId` across `server/` returns zero hits). Every
teacher sees, edits, and deletes every other teacher's lessons; the existing `[Authorize(Roles = "Teacher,Admin")]`
is a **role** check, not an ownership check. Fix: add a real **`Lesson.TeacherId` FK**, introduce a **`Course`
entity** (the teacher-managed subject list) replacing the free-text `Lesson.Name`, scope every lesson/assignment/
report query by the authenticated teacher, and regroup the student grades screen into **one average per course**.
The DB holds only 5 test lessons — they are deleted first, so no backfill migration is needed and the new FKs are
non-nullable from day one.

## User decisions (already confirmed)

- **Lessons are per-teacher.** Sara sees and edits only her own lessons. Admin sees everything.
- **Students and SchoolClasses stay shared school-wide** — explicitly NOT scoped per teacher.
- **`Lesson.Name` IS the subject** ("C#", "java"); **`Lesson.Subject` is the sub-topic** ("loops", "arrays").
  This was clarified by the user and inverts the initial reading of the schema.
- **Subject must be picked from a managed list**, not typed — the teacher is required to select an existing course,
  while still being able to add a new one when needed. Autocomplete was explicitly rejected as too loose.
- **`Lesson.Name` is deleted** — redundant once the course dropdown exists.
- **`Lesson.Subject` stays free-text** — unchanged.
- **One average per course, no overall average.** Ayala gets `C# 87` and `Java 92` from Sara, separately.
- **Delete the 5 existing test lessons** rather than migrating them.
- **In scope as a bonus fix:** students can currently read other students' lesson results (no auth check at all).

## Key facts (from codebase research)

- **JWT already carries the user id.** `JwtTokenGenerator.cs:26-35` emits `JwtRegisteredClaimNames.Sub` = `user.Id`,
  plus `ClaimTypes.Name`/`ClaimTypes.Role` and a custom `studentId` claim for students only. **No token change needed.**
  ASP.NET remaps `sub`→`ClaimTypes.NameIdentifier`, hence the `?? "sub"` fallback in `AuthController.cs:84-100`.
- **`IHttpContextAccessor` is registered nowhere; no `ICurrentUserService` exists.** Controllers read claims directly
  off `HttpContext.User`. The only existing ownership idiom is `StudentsController.IsAllowedForStudent` (`:39-46`),
  and the only existing identity-threading precedent is `LessonsController.GetAll` (`:41-51`) building
  `GetLessonsQuery(classId, studentId)`.
- `Lesson.cs` (16 lines): `Id`, `Name`, `Subject`, `LessonDate`, `TeacherName`, `CreatedAt`, navs `Assignments`
  and `Classes`. **No `Create` factory** — AutoMapper maps `RequestDto → Lesson` directly.
- Downstream of Lesson: `Assignment.LessonId`, `LessonResult.LessonId`, and the `LessonSchoolClasses` join table.
  `Submission` reaches Lesson only indirectly (`Submission.AssignmentId → Assignment.LessonId`).
- **All 5 lesson handlers have zero teacher filtering** (`GetLessons`, `GetLessonById`, `CreateLesson`,
  `UpdateLesson`, `DeleteLesson`), as do all 5 assignment handlers (they take `lessonId` and trust it).
- **Four aggregate leak sites:** `ExportGradesPeriodReportHandler:38-46` (all lessons in range, highest risk),
  `GetStudentGradesSummaryHandler:35` (`GetAllAsync(ct)` — averages across every teacher), `ExportLessonResultsHandler:37-44`
  (no ownership check), `GetRecentGradedSubmissionsHandler:24` (bell shows every teacher's graded submissions).
- **`LessonResultController.Get` (`/{studentId}/{lessonId}`, line 35) has NO authorization check** — any authenticated
  student can read any other student's result. Pre-existing bug, fixed here.
- `SubmissionController.cs` is entirely commented out; submissions are exposed via
  `StudentsController` `/api/students/{studentId}/submissions` and are already student-scoped. **No changes needed there.**
- **`AiWorker` never loads a Lesson** — it reads `submission.Assignment?.LessonId` purely as a log correlation id
  (lines 60/90/121/140). The Hangfire job is therefore safe, *provided* ownership filtering lives in handlers and
  never inside repository defaults.
- EF config is **inline in `GradeSheetContext.OnModelCreating` (lines 23-84)**; `Infrastructure/Data/Configurations/`
  is empty — no `IEntityTypeConfiguration` classes. `User.Role` is stored as a **string** (`.HasConversion<string>()`).
  The `Student→User` FK (lines 56-64) is the precedent for a User FK.
- Latest migration: `20260720141929_AddExpectedFilesAndSourceFiles`. SQLite; `dotnet ef migrations add <Name>`.
- **Actual DB contents (verified):** 5 lessons. `Name` holds values like "C# lab", "java", "algorithmics C#", "C#",
  plus one keyboard-mash entry; `Subject` holds one real topic repeated 3x, one keyboard-mash entry, and a
  near-duplicate of the real topic with a "1" appended; `TeacherName` holds 5 distinct strings of which **only 1
  matches an existing user's `FullName`**. Clearly test data. The near-duplicate subject is exactly the silent
  grouping breakage this feature must prevent.
- Client: `teacherName` appears ONLY in `lesson.model.ts` (3 interfaces) and `lesson-form.component.ts` (5 lines) —
  **it is not rendered in any list template.** `auth.service.ts` does not decode the JWT; it mirrors the login
  response body into `localStorage` and exposes `fullName`/`role`/`studentId` but **no userId**.
- Assignment creation reaches `lessonId` via the **URL** (`/lessons/{lessonId}/assignments`), not a dropdown.

## Design decisions

### Thread `teacherId` explicitly; do NOT introduce `ICurrentUserService`

The controller reads the claim and passes it into each command/query record, matching the existing `studentId`
precedent exactly. Rationale:

1. Adding an ambient service would create **two competing identity mechanisms in `LessonsController`** — `StudentId`
   from a claim read in the controller, `TeacherId` from ambient state.
2. `AiWorker` runs outside any HTTP request. A handler depending on `ICurrentUserService` would silently resolve a
   null `HttpContext` there.
3. `server/CLAUDE.md` forbids Application referencing AspNetCore and requires logic in handlers.

**Cost, stated honestly:** ~15 records gain a field, and the failure mode is **silent** — a forgotten parameter is a
leak, not a compile error. **Mitigation: `int? TeacherId` is positional and has NO default value**, so omitting it
*is* a compile error. Never write `int? TeacherId = null`. `null` means "privileged — Admin sees all".

### Ownership check lives in the handler; unauthorized returns 404

Not the controller (CLAUDE.md forbids logic/throws there), not a pipeline behavior (would load the entity twice).
A single shared helper `Application/Common/Authorization/LessonAccess.GetOwnedOrThrowAsync(repo, lessonId, teacherId, ct)`
throws `NotFoundException` for both "missing" and "not yours" — indistinguishable, so lesson ids cannot be probed.
403 would leak existence. Precedent: `GetAssignmentByIdHandler.cs:29-30` already models "wrong parent" as 404.

**Rule: role mismatch → 403 · ownership mismatch → 404.**

### `Course` is owned by the teacher

"C#" for Sara and "C#" for Ruti are separate rows. Not a problem — each teacher only ever sees her own — and it
avoids both a shared catalogue needing Admin curation and cross-teacher naming collisions.

## Database changes at a glance

**One new table — `Courses`:**

| Column | Notes |
|---|---|
| `Id` | PK |
| `Name` | "C#", "java" |
| `TeacherId` | FK → `Users`, Restrict |
| `CreatedAt` | |

Unique index on `(TeacherId, Name)` — one teacher cannot have two courses with the same name; two teachers can.

**`Lessons` — two columns dropped, two added:**

| Before | After |
|---|---|
| `Id` | `Id` |
| `Name` (TEXT, notnull) | **dropped** → `CourseId` (FK → `Courses`) |
| `Subject` | `Subject` *(unchanged)* |
| `LessonDate` | `LessonDate` |
| `TeacherName` (TEXT, notnull) | **dropped** → `TeacherId` (FK → `Users`) |
| `CreatedAt` | `CreatedAt` |

Plus non-unique indexes on `Lessons.TeacherId` and `Lessons.CourseId`.

**Every other table is structurally unchanged:** `Students`, `SchoolClasses`, `Users`, `Assignments`, `Submissions`,
`LessonResults`, `LessonSchoolClasses`, `Logs`.

## Implementation phases

### Phase 0 — Delete test data (FIRST)

**Exact scope, measured against the live DB — user confirmed deletion with no export:**

| Table | Deleted | Kept |
|---|---|---|
| `Lessons` | 5 | 0 |
| `Assignments` | 7 | 0 |
| `Submissions` | 19 | 0 |
| `LessonSchoolClasses` | 25 | 0 |
| `LessonResults` | 0 | 0 |
| `Students` | — | **42** |
| `Users` | — | **47** |
| `SchoolClasses` | — | **5** |
| `Logs` | — | untouched |

**Why this is safe:** of the 19 submissions, **10 are `AiFailed` (status 4)** and **4 are stuck in `ProcessingAi`
(status 2)**; every scored row has `Score = 0.0` and the rest are `null`. `LessonResults` is **empty** — no lesson was
ever finalized. One of the 7 assignments is literally titled `"mmm"`. There is no real grade data to lose.

Back up `server/Api/GradeSheet.db` first (precedent: `GradeSheet.backup-before-schoolclasses.db`) — it is the only
recovery path. Then, in FK-safe order:

```sql
DELETE FROM Submissions WHERE AssignmentId IN (SELECT Id FROM Assignments);
DELETE FROM LessonResults;
DELETE FROM Assignments;
DELETE FROM LessonSchoolClasses;
DELETE FROM Lessons;
```

Verify `SELECT COUNT(*) FROM Lessons` = 0 and that `Students`/`Users`/`SchoolClasses` are unchanged. This is what
makes `TeacherId`/`CourseId` non-nullable with **no backfill migration**.

**Known consequence:** students lose their submission history in the "My Journey" area. Acceptable — most of it is
failed or stuck, and none of it carries a real grade.

### Phase 1 — `Course` entity

`server/Domain/Entities/Course.cs` — protected ctor + static `Create(name, teacherId)` per CLAUDE.md:
`Id`, `Name`, `TeacherId`, `Teacher`, `CreatedAt`, `ICollection<Lesson> Lessons`.

Full CQRS CRUD under `server/Application/UseCases/Courses/` (`GetCourses`/`CreateCourse`/`UpdateCourse`/`DeleteCourse`),
all filtered by `TeacherId`. `ICourseRepository` in `Domain/Abstractions` + `Infrastructure/Repositories` impl.
`CoursesController` extends `ApiControllerBase`.

**Delete guard:** a course that still has lessons throws `BusinessRuleException` → 400. (User-facing message text is
Hebrew, per the app's existing UI language — see `client/CLAUDE.md`.)

### Phase 2 — `Lesson`: ownership + course

`server/Domain/Entities/Lesson.cs`: add `TeacherId`/`Teacher` and `CourseId`/`Course` (both required). **Delete `Name`**
(replaced by `Course.Name`) and **delete `TeacherName`**. `Subject` unchanged.

Inline in `GradeSheetContext.OnModelCreating` after line 82:

```csharp
modelBuilder.Entity<Lesson>().HasOne(l => l.Teacher).WithMany()
    .HasForeignKey(l => l.TeacherId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<Lesson>().HasOne(l => l.Course).WithMany(c => c.Lessons)
    .HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<Course>(c => {
    c.Property(x => x.Name).IsRequired().HasMaxLength(100);
    c.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    c.HasIndex(x => new { x.TeacherId, x.Name }).IsUnique();
});
modelBuilder.Entity<Lesson>().HasIndex(l => l.TeacherId);
modelBuilder.Entity<Lesson>().HasIndex(l => l.CourseId);
```

`Restrict` is deliberate — cascade would delete Assignments → Submissions → student work.

Migration: `dotnet ef migrations add AddCourseAndLessonOwnership`. No backfill.

### Phase 3 — API scaffolding

`server/Api/Controllers/ApiControllerBase.cs`:

```csharp
protected int CurrentUserId =>
    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

/// <summary>null = privileged (Admin sees all)</summary>
protected int? OwnerScopeTeacherId => User.IsInRole("Admin") ? null : CurrentUserId;
```

`server/Application/Common/Authorization/LessonAccess.cs` — `GetOwnedOrThrowAsync` as described above.

### Phase 4 — Lesson CRUD

Repository: `GetAllAsync(int? classId, int? teacherId, ct)` + `.Include(l => l.Course)`;
`GetByDateRangeAsync(from, to, int? teacherId, ct)`; **delete the parameterless `GetAllAsync(CancellationToken)`** —
it is the sharp edge behind the Phase 6 leak. `GetByIdAsync` keeps its deliberate lack of `AsNoTracking()`.

All 5 records gain `int? TeacherId`. DTOs: drop `name`/`teacherName`, add `courseId`; response gains
`courseId`+`courseName`. Validators require `CourseId`; **the "course belongs to this teacher" check happens in the
handler**, not the validator (validators have no identity).

`CreateLessonHandler` sets `lesson.TeacherId` after `_mapper.Map<Lesson>(...)`, mirroring how `CreateAssignmentHandler`
sets `LessonId`, and validates course ownership.

> **⚠️ RISK R1 — the single most important detail.** Add `.ForMember(d => d.TeacherId, opt => opt.Ignore())` to the
> `UpdateLessonRequestDto → Lesson` map. Without it AutoMapper overwrites `TeacherId` with `0` and **orphans the lesson
> on every edit.** Verify: create → edit → `TeacherId` unchanged.

Also delete the commented-out dead `Create` action at `LessonsController.cs:69-81`.

### Phase 5 — Assignments

All 5 routes are already nested under `{lessonId}` and every record already carries `LessonId`. **No route or DTO
change** — add `int? TeacherId` and one line per handler:

```csharp
await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, request.LessonId, request.TeacherId, ct);
```

`CreateAssignmentHandler`/`DeleteAssignmentHandler` already load-and-throw — net zero added lines.
`UpdateAssignmentHandler`, `GetAssignmentByIdHandler`, `GetAssignmentsHandler` need `ILessonRepository` injected.

### Phase 6 — Reports, per-course averages, bell

**Per-course averages** — `StudentGradesSummaryDto` is restructured, not just filtered. Remove the single `Average`
and the flat `Grades`; group instead:

```csharp
public class StudentGradesSummaryDto {
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public List<CourseAverageDto> Courses { get; set; } = new();
}
public class CourseAverageDto {
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";   // "C#"
    public double? Average { get; set; }
    public List<StudentGradeItemDto> Grades { get; set; } = new();
}
```

`StudentGradeItemDto`: replace `LessonName` with `Subject` (the sub-topic) — the course is already the group header.
Handler filters by `teacherId`, then `GroupBy(l => l.CourseId)`. `Grades` stays nested so a teacher can open
"C# 87" and see which lessons produced it.

**Other three leak sites:** `ExportGradesPeriodReportQuery` and `ExportLessonResultsQuery` gain `int? TeacherId`
(the latter switches to `LessonAccess`); `GetRecentGradedSubmissionsQuery` gains `int? TeacherId` and
`ISubmissionRepository.GetRecentGradedAsync(limit, teacherId, ct)` filters via the existing nav:

```csharp
if (teacherId.HasValue)
    query = query.Where(s => s.Assignment.Lesson.TeacherId == teacherId.Value);
```

**The filter must precede `.Take(limit)`** — otherwise you take the global top 20 then filter down to 3.

**`CompleteLessonCommand` AutoMapper trap:** it is built via `CreateMap<CompleteLessonRequestDto, CompleteLessonCommand>()`.
A record with a required positional param does not map cleanly — construct it explicitly at
`LessonResultController.cs:57` and delete the `CreateMap` (grep confirms a single caller).

**Bonus bug fix:** add an `IsAllowedForStudent`-shaped check to `LessonResultController.Get` — student → self only;
teacher → `LessonAccess` on the lesson.

### Phase 7 — Client

The client needs **no knowledge of the teacher's userId** — the server derives ownership from the token. Do not add
`userId` to `StoredUser`; it would be dead state that can drift.

1. **New courses screen** under `client/src/app/pages/courses/` + `courses.service.ts` + `course.model.ts`, per
   `client-list-table-pattern`. Add a nav entry.
2. `lesson.model.ts` — drop `name`/`teacherName`; add `courseId` (Create/Update) and `courseId`+`courseName` (Response).
3. `lesson-form.component.ts` — delete the `name` and `teacherName` fields (template 119-139, control 215, patch 261,
   payload 291); add a **required `p-dropdown`** of courses. `subject` stays free-text.
   - **Quick-add:** an "add new course" button beside the dropdown opens a small dialog, creates, and auto-selects —
     this satisfies the "can add one when needed" requirement without leaving the form.
   - **Empty state:** if the teacher has no courses, show a message linking to the courses screen (see R4).
4. `lessons-list.component.ts` — a course column (`courseName`) replacing `name`, included in the filter (line 422)
   and sorting.
5. Student grades screen — flat list becomes grouped-by-course with a per-group average.
6. `my-grades.component.ts:68` / `my-lessons-list.component.ts:73` render `subject`; add `courseName`.

### Phase 8 — Regression sweep

Student area end-to-end (`/my/lessons` → `/my/assignments` → submit → `/my/grades` → feedback); confirm the Hangfire
job still reaches `Submission.Status = Done`; two teachers each see only their own courses and lessons; a student with
grades in 2 of one teacher's courses shows 2 separate averages; a lesson cannot be saved without a course;
`dotnet build` + `ng build`.

## Risks

- **R1 (high) — AutoMapper nulls `TeacherId` on update.** See Phase 4. The number-one risk in this plan.
- **R2 (high) — student-area regression.** If `GetLessonsHandler` applies the teacher filter unconditionally, students
  see zero lessons. The two filters are **mutually exclusive**: `StudentId.HasValue` → class scoping, teacher filter
  NOT applied. Encode as an explicit early-return branch, not two chained `if`s.
- **R3 (medium) — background job.** Verified safe: `AiWorker` never resolves `ILessonRepository`. This holds **only**
  because ownership lives in handlers. **Never push the `TeacherId` filter into a repository default.**
- **R4 (medium) — required `CourseId` blocks lesson creation** until a course exists. Mitigated by the in-form
  quick-add; without it a new teacher hits a dead end.
- **R5 (medium) — deleting `Lesson.Name`** touches Excel reports, student screens, and every display site.
  Grep `lesson.name` / `LessonName` first and confirm each moved to `courseName`.
- **R6 (low) — the `null` convention is silent.** `null` = "sees everything" is easy to misuse. Mitigated by the
  non-optional parameter + XML doc. This is the honest cost of the chosen approach.
- **R7 (low) — removing the `CreateMap` breaks at runtime, not compile time.** Grep confirms one caller.
