# Plan: Excel Export/Import (Teachers only)

## TL;DR

Teachers get **"ייצוא לאקסל"** on the Students list and on the Lesson-Results (grades) screen, plus **"ייבוא מאקסל"**
(students only: full name + class) on the Students list. Excel files (.xlsx) are generated **server-side with ClosedXML**
(real Excel, Hebrew RTL, no encoding issues). All three endpoints are `[Authorize(Roles = "Teacher")]`; the client pages
are already behind `teacherGuard`, so students never see the buttons.

## User decisions (already confirmed)

- Export scope: Students list + Lesson Results (grades table per lesson). NOT lessons/assignments/submissions.
- Import scope: Students only — columns: שם מלא | כיתה. No account creation on import (future extension).
- Excel generation: server-side with ClosedXML (not client-side SheetJS).
- Import model: partial success — valid rows are imported, bad rows reported with row numbers (not all-or-nothing).

## Key facts (from codebase research)

- No Excel/CSV package exists in any .csproj; no FileResult usage in any controller yet.
- No bulk-create endpoint; students created one-by-one via `POST /api/students` (`CreateStudentCommand`).
- `CreateStudentCommandValidator`: fullName NotEmpty/Max 100, className NotEmpty/Max 50 — reuse the same rules per row on import.
- Lesson-results client computes the grades matrix by looping `GET /api/lesson-results/{studentId}/{lessonId}` per student
  (`client/src/app/services/lesson-results.service.ts`); server has `GetLessonResultQuery` that computes
  TotalAssignments/CompletedAssignments on the fly. Export handler needs a server-side aggregation
  (likely new `ILessonResultRepository.GetByLessonIdAsync(lessonId)` + students list).
- Client `ApiClient` (`client/src/app/core/http/api-client.ts`) is a thin HttpClient wrapper; no blob/multipart
  patterns exist yet. `authInterceptor` adds the Bearer token automatically (also to blob requests).
- Toolbars: Students list header has primary "סטודנטית חדשה" button; lesson-results has a card header —
  see `client/src/app/pages/students/students-list.component.ts`, `client/src/app/pages/lesson-results/lesson-results-list.component.ts`.
- Running Api process locks build output — stop it before `dotnet build`.
- xlsx contentType: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

## Button placement (decided)

| Screen         | Buttons                                                                       | Placement                                                                |
| -------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Students list  | "ייצוא לאקסל" (secondary, pi-download) + "ייבוא מאקסל" (secondary, pi-upload) | Page header, beside the primary "סטודנטית חדשה" (primary stays dominant) |
| Lesson results | "ייצוא לאקסל" only                                                            | Card header, near the lesson title                                       |

## Steps

### Phase 1 — Server: package + export use cases

> Skills: `backend-mediatr-query-handler-pattern`, `backend-repository-query-pattern`

1. Add **ClosedXML** NuGet to `server/Application/Application.csproj`.
2. `ExportStudentsQuery : IRequest<byte[]>` + handler in `server/Application/UseCases/Student/ExportStudents/` —
   uses `IStudentRepository.GetAllAsync`; builds an RTL worksheet (`ws.RightToLeft = true`) with header row:
   שם מלא, כיתה, מס' הגשות, מס' ציונים, יש חשבון, תאריך יצירה. Bold header, auto-fit columns.
3. `ExportLessonResultsQuery(int LessonId) : IRequest<byte[]>` + handler in
   `server/Application/UseCases/LessonResults/ExportLessonResults/` — reuses the computation logic of
   `GetLessonResultHandler`; add `ILessonResultRepository.GetByLessonIdAsync(lessonId)`
   (Domain/Abstractions interface + Infrastructure implementation, AsNoTracking). Columns:
   שם תלמידה, תרגילים שהושלמו (X/Y), ציון סופי, סטטוס. `NotFoundException` if lesson doesn't exist.

### Phase 2 — Server: import use case

> Skills: `backend-mediatr-query-handler-pattern`

4. `ImportStudentsCommand(Stream FileStream) : IRequest<ImportStudentsResultDto>` + handler in
   `server/Application/UseCases/Student/ImportStudents/` — parses xlsx with ClosedXML (first sheet,
   header row: שם מלא | כיתה); per-row validation mirroring `CreateStudentCommandValidator`
   (fullName NotEmpty/Max100, className NotEmpty/Max50); creates valid rows via `IStudentRepository` + `IUnitOfWork`;
   returns `ImportStudentsResultDto { CreatedCount, Errors: [{ RowNumber, Message }] }` (new DTO in
   `server/Application/Dtos/Student/`). Application layer must NOT reference AspNetCore — controller passes a Stream.

### Phase 3 — Server: controller endpoints

> Skills: `backend-controller-endpoint-pattern`

5. All `[Authorize(Roles = "Teacher")]`:
   - `GET /api/students/export` (StudentsController) → `File(bytes, xlsxContentType, "students.xlsx")`
   - `POST /api/students/import` (StudentsController, `IFormFile`; validate .xlsx extension + ~5MB limit; 400 on bad file) → `ImportStudentsResultDto`
   - `GET /api/lesson-results/lesson/{lessonId:int}/export` (LessonResultController) → `File(...)`, filename `lesson-{id}-results.xlsx`
6. Verify: stop Api process → `dotnet build server/SmartGrader.sln`.

### Phase 4 — Client: services + export buttons

> Skills: `client-list-table-pattern` (button placement), `client-design-token-rollout-pattern` (sg-\* styling)

7. `students.service.ts`: `exportExcel(): Observable<Blob>` via `http.get(url, { responseType: 'blob' })`;
   `importExcel(file: File)` via FormData POST. `lesson-results.service.ts`: `exportExcel(lessonId)`.
8. Small blob-download helper (blob → temporary `<a download>`) — tiny util or inline per component.
9. Students list header: add the two secondary sg-btn buttons beside the primary; loading spinner on the button
   during download. Lesson-results card header: export button. Success/failure toasts via `MessageService`
   (ApiErrorInterceptor covers server errors).

### Phase 5 — Client: import dialog

> Skills: `client-flow-fix-implementation-pattern` (Hebrew gender-neutral copy, toast/confirm conventions)

10. `p-dialog` on the students list: file picker (accept=".xlsx"), short format explanation (שם מלא | כיתה),
    upload with progress state; on result — show CreatedCount + per-row errors table; success toast; reload list.
11. Copy: Hebrew only, gender-neutral per client conventions.

### Phase 6 — Verification

12. `dotnet build` clean; `ng build` clean.
13. Manual: teacher token (teacher@test.com / Password123) → both exports download and open in Excel with correct Hebrew RTL;
    student token (noa@test.com) → **403** on all 3 endpoints.
14. Import: valid file creates students + toast; file with bad rows shows row-numbered errors; non-xlsx rejected (400).
15. UI check at 360/768/1280, RTL.

## Skills & agents map

| Phase                               | Skill / Agent                                                                                                                                                      |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1–2 (queries/command + repo method) | `backend-mediatr-query-handler-pattern`, `backend-repository-query-pattern` (or subagents `phase-query-handler-implementation`, `phase-repository-implementation`) |
| 3 (endpoints)                       | `backend-controller-endpoint-pattern`                                                                                                                              |
| 4 (buttons/toolbar)                 | `client-list-table-pattern`, `client-design-token-rollout-pattern`                                                                                                 |
| 5 (dialog + copy)                   | `client-flow-fix-implementation-pattern`                                                                                                                           |
| research (if needed)                | `Explore` subagent                                                                                                                                                 |

Not needed: `backend-automapper-profile-pattern` (no new entity↔DTO mapping — export rows and import result are built by hand),
`backend-hebrew-calendar-pattern`, CVA/student-area/UX-doc skills.

## Further considerations

- "הורדת תבנית ריקה" button inside the import dialog (cheap: same export code with zero rows) — optional nice-to-have.
- Import with account creation (email+password columns) — future extension, reuse `CreateStudentAccountCommand`.
