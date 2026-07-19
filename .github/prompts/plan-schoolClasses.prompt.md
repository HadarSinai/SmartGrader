# Plan: SchoolClass Entity + Year Rollover (כיתות ושנות לימודים)

## TL;DR

Today `Student.ClassName` is a free-text string, lessons are global (visible to every student forever), and the
only cleanup path is hard-delete (loses all grades). Next school year the same class names ("י'1") will be reused
for NEW students and cohorts will mix. Fix: introduce a real **`SchoolClass` entity** (Name + Hebrew academic year
+ `IsArchived`), a required **`Student.ClassId` FK**, a **Lesson↔SchoolClass many-to-many** (a lesson can target
several classes), a one-click **"סיום שנה"** bulk-archive action, and **read-only archive viewing**. A data
migration converts existing ClassName strings into class records — no grade/submission is lost.

## User decisions (already confirmed)

- Approach B chosen: full Class entity (over "year field on Student" or "archive-flag only").
- A lesson can be assigned to **more than one class** (many-to-many).
- Scope includes: bulk "finish year" action + archive viewing.
- **Out of scope** (explicitly not selected): academic-year column in the Excel import template; blocking logins
  of archived students.

## Key facts (from codebase research)

- `Student.cs` (server/Domain/Entities): `Id` (private set), `FullName`, `ClassName` (string, max 50 per validator),
  `CreatedAt`, `UserId?`/`User?`, navs `Submissions`, `LessonResults`. Protected parameterless ctor.
- `Lesson.cs`: `Id`, `Name`, `Subject`, `LessonDate`, `TeacherName`, `CreatedAt`, nav `ICollection<Assignment>`.
  **No class reference at all** — every lesson is effectively broadcast to all students.
- DB: SQLite via EF Core, `GradeSheetContext.cs` (server/Infrastructure/Data). All relations are one-to-many;
  **no join table exists yet** — Lesson↔Class will be the repo's first many-to-many (use EF Core 8 skip navigations,
  implicit join table).
- Delete = hard delete with CASCADE (`DeleteStudentHandler` also deletes the linked User). No IsActive/IsArchived
  flag anywhere.
- Client class filter (`students-list.component.ts`) derives options from `students.map(s => s.className)` —
  purely client-side.
- Student area: `MyLessonsListComponent` fetches **all** lessons via `lessonsService.getAll()`, then per-lesson
  `lessonResultsService.getResult(studentId, lesson.id)` with 404→"בתהליך". `studentId` ALWAYS from
  `AuthService.studentId()` token claim, never URL.
- Excel import (`ImportStudentsHandler`, ClosedXML): columns שם מלא | כיתה | שם משתמש | סיסמה (1-indexed cells),
  per-row validation with RowNumber errors, single SaveChanges at the end.
- Hebrew year formatting (gematria, e.g. "תשפ"ו") already exists — `HebrewDateConverter`
  (server/Application/Common/HebrewDate), per plan-hebrewDates.
- JWT claims: `ClaimTypes.Role` "Teacher"/"Student", `studentId` (students only). Writes are
  `[Authorize(Roles="Teacher")]`.
- Gotchas: repos use AsNoTracking (updates via `context.Update`); stop the running Api process before
  `dotnet build`/`dotnet ef`; dev accounts teacher@test.com / noa@test.com (student 13), password Password123.

## Design decisions

- `SchoolClass`: `Id`, `Name` (string, max 50), `AcademicYear` (**int Hebrew year**, e.g. 5786; displayed as
  gematria via `HebrewDateConverter`), `IsArchived` (bool, default false), `CreatedAt`.
  Unique index on (`Name`, `AcademicYear`).
- `Student.ClassId` is **required**; `StudentResponseDto.ClassName` stays as a computed string (`Class.Name`)
  so most of the client keeps working; add `ClassId` + `ClassIsArchived` to the DTO.
- Migration data conversion (raw SQL in `Up()`): each distinct existing `ClassName` → one SchoolClass in the
  current academic year (5786/תשפ"ו); set `Student.ClassId`; link **existing lessons to ALL created classes**
  (backward compat — nothing disappears); drop `Students.ClassName`.
- New/updated lessons require **≥1 class** (FluentValidation). `GET /api/lessons` with a Student token returns
  only lessons linked to that student's class (claim-based, server-side).
- Class delete allowed only when it has no students (validation error otherwise); archiving is the normal path.
- Finish-year: `POST /api/classes/finish-year` sets `IsArchived = true` on **all** active classes. Default list
  queries (classes, students) exclude archived; `includeArchived=true` query param exposes them for archive view.
- Excel import: class column is looked up among **active classes of the current year** by name;
  **auto-create** if missing (teacher convenience).

## Steps

### Phase 1 — Backend foundation (blocks everything else)

1. Domain: new `server/Domain/Entities/SchoolClass.cs` (`Id` private set, `Name`, `AcademicYear`, `IsArchived`,
   `CreatedAt`, navs `ICollection<Student>` + `ICollection<Lesson>`); add `ClassId` + `Class` nav to `Student.cs`;
   add `ICollection<SchoolClass> Classes` to `Lesson.cs`.
2. Domain/Abstractions: `ISchoolClassRepository` — `GetAllAsync(bool includeArchived, ct)`, `GetByIdAsync`,
   `GetByNameAndYearAsync(name, year, ct)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ArchiveAllActiveAsync(ct)`
   (follow backend-repository-query-pattern).
3. Infrastructure: `GradeSheetContext` — `DbSet<SchoolClass>`, OnModelCreating: unique index (Name, AcademicYear),
   Student→Class required FK with `DeleteBehavior.Restrict`, Lesson↔SchoolClass many-to-many (skip navigations);
   `SchoolClassRepository` (AsNoTracking pattern like StudentRepository); DI registration in
   `Infrastructure/DependencyInjection.cs`.
4. Migration `AddSchoolClasses` including the data-conversion SQL described above. Verify with
   `dotnet ef database update` on the dev DB: every existing student has a ClassId, ClassName column gone,
   counts unchanged.

### Phase 2 — Classes management (API + client) and student flows

5. Application: `Dtos/Classes/` — `SchoolClassResponseDto` (`Id`, `Name`, `AcademicYear`,
   `AcademicYearHebrew` string, `IsArchived`, `StudentsCount`), `CreateClassRequestDto` / `UpdateClassRequestDto`
   (`Name`, `AcademicYear`); `UseCases/Classes/` — GetClasses(includeArchived) / GetClassById / CreateClass /
   UpdateClass / DeleteClass (delete blocked when StudentsCount > 0); `SchoolClassProfile` (AutoMapper, gematria
   year via HebrewDateConverter); FluentValidation validators (name required ≤50, year 5000–6000, uniqueness).
6. `ClassesController` (`api/classes`) — `[Authorize(Roles="Teacher")]` on everything
   (follow backend-controller-endpoint-pattern).
7. Student flows: `Create/UpdateStudentRequestDto` — `ClassName` → `ClassId`; handlers validate the class exists
   and is not archived; `StudentResponseDto` adds `ClassId`/`ClassIsArchived`, keeps `ClassName` mapped from
   `Class.Name`; `GetStudentsQuery` gets `IncludeArchived` (filters on `Class.IsArchived`); StudentRepository
   `Include(s => s.Class)`.
8. `ImportStudentsHandler`: resolve כיתה cell → active class of the current year by name, auto-create when missing
   (current year = Hebrew year of today via HebrewDateConverter). Keep per-row RowNumber errors.
9. Client: `models/class.model.ts`, `services/classes.service.ts`, `pages/classes/classes-list.component.ts` +
   `class-form.component.ts` (follow client-list-table-pattern + spec.md design tokens), routes in `app.routes.ts`
   (teacherGuard) + nav link in AppLayout. `student-form.component.ts`: className text input → `p-dropdown` of
   active classes (sends `classId`). Students list: class filter options come from ClassesService (not derived).

### Phase 3 — Lesson↔classes + student-area filtering

10. Lesson DTOs: `Create/UpdateLessonRequestDto` add `List<int> ClassIds` (validator: min 1, all exist & active);
    `LessonResponseDto` adds `Classes` (id+name pairs) and a display string. Create/UpdateLessonHandler resolve
    the SchoolClass entities and set the navigation (AutoMapper ignores the collection; handler assigns).
11. `GET /api/lessons`: when caller role is Student — take `studentId` claim → student's ClassId → return only
    lessons linked to that class. Teacher gets all + optional `classId` filter.
12. Client: `lesson.model.ts` updated; lesson-form gets `p-multiSelect` of active classes; lessons-list shows
    class names (chips/joined string) + optional class filter. `MyLessonsListComponent` / my-grades need no code
    change — server-side filtering now applies (verify both).

### Phase 4 — Finish year + archive view

13. Backend: `FinishYearCommand/Handler` (archives all active classes via `ArchiveAllActiveAsync`),
    `POST /api/classes/finish-year` (Teacher only).
14. Client: "סיום שנה" button on the classes list with a strong `ConfirmationService` dialog (shows how many
    classes/students will be archived); archive toggle (e.g. `p-toggleButton`) on classes + students lists —
    archived rows are read-only (no edit/delete/actions) and visually muted. Historical grades of archived
    students remain viewable through the existing screens.

## Verification

1. Stop the Api process; `dotnet build` in server/; run the migration on the dev SQLite; check students got
   ClassId, ClassName column dropped, submission/result counts unchanged.
2. Swagger smoke: classes CRUD; create student with classId; Excel import with an existing + a new class name;
   create a lesson with 2 classes; `GET /api/lessons` with a Student token (noa@test.com) returns only her
   class's lessons.
3. `POST /api/classes/finish-year` → default lists empty, `includeArchived=true` shows everything, lesson results
   of archived students still load.
4. Client: `ng build`; manual flows in RTL at 360/768/1280 — classes list/form, student form dropdown, lesson
   multi-select, "המסע שלי" filtered lessons, archive toggle + finish-year dialog.

## Relevant skills

backend-repository-query-pattern, backend-mediatr-query-handler-pattern, backend-controller-endpoint-pattern,
backend-automapper-profile-pattern, backend-hebrew-calendar-pattern, backend-excel-closedxml-pattern,
client-list-table-pattern, client-design-token-rollout-pattern.
