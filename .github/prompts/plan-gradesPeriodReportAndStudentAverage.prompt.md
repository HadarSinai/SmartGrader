# Plan: Grades Period Report (Excel) + Student Grades Average

## TL;DR

Two teacher-facing reporting features:

1. **Period grades report** — an Excel (.xlsx) export of **all students' final grades** for every lesson whose
   `LessonDate` falls within a chosen period (from/to). Matrix layout: one row per student, one column per lesson,
   plus a per-student **average** column (average of that student's final scores).
2. **Student grades detail (row click)** — clicking a **student row** opens that student's final grades
   (one row per lesson, `LessonResult.FinalScore`) with the **computed average** — JSON endpoint + client dialog.

Excel generated server-side with ClosedXML (existing pattern), endpoints `[Authorize(Roles = "Teacher,Admin")]`.

## User decisions (confirmed)

- Report scope: **final grades only** (`LessonResult.FinalScore`) per student per lesson in the period — not raw submissions.
- The average is **per student**: the average of her final grades across lessons. It appears in two places —
  the last column of the Excel matrix, and interactively when clicking a student row in the client.
- Excel generation: server-side with ClosedXML (reuse the existing export pattern; package already installed).
- Period selection: the system works with **Hebrew dates** — the client sends Hebrew date components and the server
  converts with the existing `HebrewDateConverter` (same as lesson create/edit).
- The row-click detail view shows **all** the student's lesson results (no period filter) — simplest useful scope.

## Key facts (from codebase research)

- `Lesson.LessonDate` is a Gregorian `DateTime` (`server/Domain/Entities/Lesson.cs`), stored from Hebrew components
  via `HebrewDateConverter` (see `backend-hebrew-calendar-pattern` skill).
- `LessonResult` has `FinalScore` (double?, max 150 with bonus), `IsComplete`, `StudentId`, `LessonId`.
- ClosedXML is already referenced in `server/Application/Application.csproj`; a full working export example exists:
  `server/Application/UseCases/LessonResults/ExportLessonResults/ExportLessonResultsHandler.cs` +
  `GET /api/lesson-results/lesson/{lessonId:int}/export` in `LessonResultController` (RTL sheet, bold header, `File(...)`).
- Repositories — existing methods (`server/Domain/Abstractions/`):
  - `ILessonRepository`: `GetAllAsync`, `GetByIdAsync` — **no date-range method yet**.
  - `ILessonResultRepository`: `GetAsync(studentId, lessonId)`, `GetByLessonIdAsync(lessonId)`, `AddAsync` —
    **no `GetByStudentIdAsync` yet**.
  - `IStudentRepository`: `GetAllAsync`, `GetByIdAsync`, `GetByUserIdAsync`.
- Client blob-download pattern already exists: `lesson-results.service.ts#exportExcel` (`responseType: 'blob'`) +
  `downloadBlob` helper in `client/src/app/core/utils/download.ts`; export button with `[loading]` on the
  lesson-results card header.
- Client Hebrew date picker CVA exists: `client/src/app/components/hebrew-date-picker/hebrew-date-picker.component.ts`
  (`HebrewDateValue { hebrewYear, hebrewMonth, hebrewDay }`, `getHebrewToday()` helper).
- Running Api process locks build output — stop it before `dotnet build`.
- xlsx contentType: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

## Steps

### Phase 1 — Server: repository methods

> Skills: `backend-repository-query-pattern`

1. `ILessonRepository.GetByDateRangeAsync(DateTime from, DateTime to, ct)` — interface + EF implementation:
   `Where(l => l.LessonDate >= from && l.LessonDate <= to)`, `AsNoTracking`, ordered by `LessonDate`.
2. `ILessonResultRepository.GetByLessonIdsAsync(IReadOnlyList<int> lessonIds, ct)` — one query instead of N
   (`Where(r => lessonIds.Contains(r.LessonId))`, `AsNoTracking`).
3. `ILessonResultRepository.GetByStudentIdAsync(int studentId, ct)` — `Where(r => r.StudentId == studentId)`,
   `AsNoTracking` (for the row-click detail view).

### Phase 2 — Server: period report use case

> Skills: `backend-mediatr-query-handler-pattern`, `backend-excel-closedxml-pattern`, `backend-hebrew-calendar-pattern`

4. `ExportGradesPeriodReportQuery(int FromHebrewYear, int FromHebrewMonth, int FromHebrewDay, int ToHebrewYear, int ToHebrewMonth, int ToHebrewDay) : IRequest<byte[]>`
   in `server/Application/UseCases/LessonResults/ExportGradesPeriodReport/`.
   Validator: all components > 0; handler converts both ends with `HebrewDateConverter` (to-date inclusive — use end of day)
   and throws `BusinessRuleException` if `from > to`.
5. Handler:
   - `lessons = GetByDateRangeAsync(from, to)`; if empty → `BusinessRuleException("אין שיעורים בתקופה שנבחרה")` (400).
   - `students = IStudentRepository.GetAllAsync()`; `results = GetByLessonIdsAsync(lessonIds)` → dictionary by `(StudentId, LessonId)`.
   - **Sheet "ציונים סופיים"** (RTL, bold header): column 1 = שם תלמידה; one column per lesson
     (header: lesson name + Hebrew date via `HebrewDateConverter.Format`); cell = `FinalScore` or empty;
     last column = **ממוצע** (average of the student's non-null final scores, rounded to 1 decimal, empty if none) —
     bold column, matching the row-click average in the client.
   - `Columns().AdjustToContents()`, save to `MemoryStream`, return bytes.

### Phase 3 — Server: student grades summary use case (row click)

> Skills: `backend-mediatr-query-handler-pattern`

6. `GetStudentGradesSummaryQuery(int StudentId) : IRequest<StudentGradesSummaryDto>` in
   `server/Application/UseCases/LessonResults/GetStudentGradesSummary/`; validator `StudentId > 0`;
   `NotFoundException` if the student doesn't exist.
7. `StudentGradesSummaryDto { int StudentId; string StudentName; double? Average; List<StudentGradeItemDto> Grades }`
   with `StudentGradeItemDto { int LessonId; string LessonName; string LessonDateHebrew; double? FinalScore; bool IsComplete }`
   in `server/Application/Dtos/LessonResults/` — handler joins `GetByStudentIdAsync(studentId)` with lesson names/dates
   (`ILessonRepository`), computes `Average` over non-null `FinalScore`s (rounded to 1 decimal, `null` if none).

### Phase 4 — Server: controller endpoints

> Skills: `backend-controller-endpoint-pattern`

8. Both in `LessonResultController`, `[Authorize(Roles = "Teacher,Admin")]`:
   - `GET /api/lesson-results/export-report?fromYear=&fromMonth=&fromDay=&toYear=&toMonth=&toDay=`
     → `File(bytes, xlsxContentType, "grades-report.xlsx")`.
   - `GET /api/lesson-results/student/{studentId:int}/summary` → `StudentGradesSummaryDto`.
     (Route coexists safely with `{studentId:int}/{lessonId:int}` thanks to the literal `student` segment.)
9. Verify: stop the running Api process → `dotnet build server/SmartGrader.sln` — 0 errors.

### Phase 5 — Client: period report dialog + download

> Skills: `client-file-download-upload-pattern`, `client-cva-form-control-pattern` (reuse picker, don't rebuild)

10. `lesson-results.service.ts`: `exportPeriodReport(from: HebrewDateValue, to: HebrewDateValue): Observable<Blob>`
    (query params, `responseType: 'blob'`).
11. Lessons list header (`lessons-list.component.ts`): secondary button **"דוח ציונים"** (pi-file-excel) opening a
    `p-dialog` with two `hebrew-date-picker` controls (מתאריך / עד תאריך, both required, default: from = start of
    current Hebrew year, to = `getHebrewToday()`), a primary "ייצוא" button with `[loading]`, `downloadBlob` on success,
    success/error toasts via `MessageService` (ApiErrorInterceptor covers server errors — including the
    "no lessons in period" 400).
12. Copy: Hebrew only, gender-neutral per client conventions.

### Phase 6 — Client: student row click → grades + average dialog

> Skills: `client-list-table-pattern`

13. `lesson-results.service.ts`: `getStudentSummary(studentId): Observable<StudentGradesSummaryDto>` +
    models in `lesson-result.model.ts`.
14. Students list (`students-list.component.ts`): clicking a student row (not the checkbox/actions cells) opens a
    `p-dialog` titled with the student's name, containing a small table — שיעור | תאריך | ציון סופי | סטטוס —
    and a prominent stat: **"ממוצע ציונים סופיים: X"** ("אין עדיין ציונים סופיים" when `average === null`).
    Loading spinner while fetching; row gets `cursor: pointer` + hover per design tokens.

### Phase 7 — Verification

15. `dotnet build` clean; `ng build` clean.
16. Manual: teacher token → report downloads, opens RTL in Excel, matrix + per-student average column correct;
    period with no lessons → clear Hebrew error toast; student token → **403** on both endpoints.
17. Row click: dialog average matches a hand-computed value for a known student; student with no final grades
    shows the empty state.
18. UI check at 360/768/1280, RTL.
