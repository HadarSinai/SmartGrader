# Plan: Per-Assignment GradingMode (FullProgram / Method / MultiFileMethod)

## TL;DR

The course progresses in stages — everything-in-`Main` (loops, arrays, matrices) → functions (incl. recursion) → objects across multiple files — but the grading pipeline only supports two paths, and **both assume the student submits a method**. A complete program with the student's own `Main` collides with the generated wrapper and fails compilation. The fix: a `GradingMode` enum on `Assignment` (`FullProgram` | `Method` | `MultiFileMethod`) that the teacher picks in the assignment form. It drives (a) how the runner wraps/executes the code, (b) which fields the teacher form requires, and (c) the instructions + placeholder the student sees on the submit screen — so every stage of the course grades correctly and the student always knows what shape of code to submit.

## User decisions (already confirmed)

1. **`FullProgram` supports multiple files too** — when the teacher defines expected files, the student uploads one per slot; files are concatenated as-is and the **student's own `Main`** runs (no wrapper). With no expected files — a single code textarea.
2. **Default mode for a new assignment: `FullProgram`** (matches the start of the course).
3. Existing assignments are migrated by inference: `ExpectedFilesJson != '[]'` → `MultiFileMethod`, otherwise → `Method` (exact behavior preservation).

## The three modes

| Enum value | Hebrew UI label | Wrapper | `TestCase.Input` format | Requirements |
|---|---|---|---|---|
| `FullProgram` | תוכנית שלמה (עם Main) | None — code runs as-is | Full stdin (each line = one `ReadLine`) | MethodName irrelevant; ExpectedFiles optional |
| `Method` | מתודה בודדת | Existing `StudentSolution` wrapper | Space-separated values | MethodName required |
| `MultiFileMethod` | פרויקט רב־קובצי (מתודת כניסה) | Existing concatenation + wrapper `Main` | JSON array of arguments | ExpectedFiles required, at least one with MethodName |

## Key facts (from codebase research)

- Runner: `server/Infrastructure/Services/CodeRunner/Judge0CodeRunner.cs` — single-file path wraps the code in `static class StudentSolution` + generated `Main` calling `MethodName`; multi-file path concatenates files + wrapper `Main` calling the entry method from `ExpectedFiles`. Shared `RunTestsAsync` posts to Judge0 with `stdin`/`expected_output`.
- Dispatch: `server/Api/BackgroundServices/AiWorker.cs` picks the path by `assignment.ExpectedFiles.Count > 0`.
- Enum-as-string precedent: `User.Role` uses `HasConversion<string>()` in `GradeSheetContext`; `SubmissionResponseDto.Status` is a `string` on the DTO.
- Student submit screen: `client/src/app/pages/my/submit-code.component.ts` — hint is hard-coded to "method body only"; multi-file slots appear when `expectedFiles.length > 0`.
- Teacher form: `client/src/app/pages/assignments/assignment-form.component.ts` — `methodName` is always required; expected-files editor always visible.
- Known latent bug: multi-file concatenation keeps student `using` lines in place, so a `using` at the top of the second file lands after the first file's classes → CS1529 even for correct code.

## Backend

### 1. Domain + EF

- New enum `GradingMode` in `server/Domain/Entities/GradingMode.cs`: `FullProgram, Method, MultiFileMethod`.
- `Assignment.GradingMode` property (`server/Domain/Entities/Assignment.cs`).
- `GradeSheetContext`: `HasConversion<string>()` (same pattern as `User.Role`).
- Migration `AddAssignmentGradingMode`: TEXT column, default `'Method'`, then `migrationBuilder.Sql` → `UPDATE Assignments SET GradingMode = 'MultiFileMethod' WHERE ExpectedFilesJson IS NOT NULL AND ExpectedFilesJson != '[]'`.

### 2. DTOs + Mapping

- Add `public string GradingMode { get; set; }` to `CreateAssignmentRequestDto`, `UpdateAssignmentRequestDto`, `AssignmentResponseDto` (`server/Application/Dtos/Assignments/`).
- `AssignmentProfile`: AutoMapper maps enum↔string automatically; no `ForMember` needed (validator guarantees a valid value first).
- `UpdateAssignmentHandler` assigns fields manually — add `assignment.GradingMode = Enum.Parse<GradingMode>(dto.GradingMode, true)`.

### 3. Validators (Create + Update Assignment)

- `GradingMode`: `NotEmpty` + `IsEnumName(typeof(GradingMode), caseSensitive: false)`.
- When `Method`: `MethodName` NotEmpty.
- When `MultiFileMethod`: `ExpectedFiles` NotEmpty; every `FileName` NotEmpty; at least one entry with `MethodName`.
- When `FullProgram`: no MethodName requirement; `ExpectedFiles` may be empty or populated (per-row `FileName` NotEmpty when present).

### 4. Runner — "no wrapper" path + usings fix

`server/Application/Services/CodeRunner/ICodeRunnerService.cs` + `Judge0CodeRunner.cs`:

- New method `Task<RunnerResult> RunProgramAsync(IReadOnlyList<SubmissionFile> sourceFiles, IReadOnlyList<TestCase> tests, CancellationToken ct)` — merges files with **no wrapper**, reuses `RunTestsAsync` (stdin = `test.Input` as-is, multi-line included).
- Shared helper `MergeFiles`: hoists top-level `using X;` directives from all files (regex), dedupes, emits them first, then the file bodies.
- Fix the existing multi-file path (`BuildWrappedMultiFileSource`) to use the same `MergeFiles` — kills the CS1529 bug.

### 5. AiWorker — dispatch by mode

Replace the `ExpectedFiles.Count > 0` condition with a switch on `assignment.GradingMode`:

- `FullProgram`: if `submission.SourceFiles` is empty, wrap `SourceCode` as `SubmissionFile("Program.cs", …)`; call `RunProgramAsync`.
- `Method`: existing single-file path.
- `MultiFileMethod`: existing multi-file path.

Adjacent fix: `_feedback.GetFeedbackAsync` receives `submission.SourceCode`, which is empty for multi-file submissions — pass the merged file contents when `SourceCode` is empty.

### 6. CreateSubmission — no contract change

Student payload unchanged. The existing submitted-files-vs-ExpectedFiles check in `CreateSubmissionHandler` now also covers FullProgram-with-files — stays as-is.

## Client — teacher side (`assignment-form.component.ts`)

- New `gradingMode` control (default `"FullProgram"`), rendered as `p-selectButton` with the 3 Hebrew labels above.
- Conditional display: `Method` → show "שם המתודה" (required only then; toggle the validator on `valueChanges`); `MultiFileMethod` → expected-files editor required (≥1 file, per-file method name shown); `FullProgram` → method name hidden, expected-files editor optional ("להגשה של כמה קבצים — התוכנית תורץ עם ה-Main של התלמיד"), per-file method-name input hidden.
- Test-cases hint per mode (Input semantics differ): FullProgram "קלט מלא לתוכנית — כל שורה נקראת ב-Console.ReadLine() אחד"; Method "ערכי הפרמטרים מופרדים ברווח, למשל: 3 5"; MultiFileMethod "מערך JSON של ארגומנטים, למשל: [3, 5]".
- Edit mode: `patchValue` includes `gradingMode`.
- `client/src/app/models/assignment.model.ts`: `export type GradingMode = "FullProgram" | "Method" | "MultiFileMethod"` + field on all 3 interfaces.

## Client — student side (`submit-code.component.ts`)

All instructions derive from `assignment.gradingMode` (this solves "the student doesn't know what's expected"):

- `isMultiFile` stays `expectedFiles.length > 0` (covers FullProgram-with-files and MultiFileMethod).
- Per-mode hint + placeholder: `FullProgram` single-file → "יש להגיש תוכנית שלמה — כולל using, class ו-Main…" with a full-program skeleton placeholder; `FullProgram` multi-file → file slots + "התוכנית תורץ כמו שהיא, עם ה-Main שכתבת"; `Method` → existing method-body hint; `MultiFileMethod` → existing message + entry-method name.
- Header shows "שם המתודה" only in `Method`/`MultiFileMethod`.
- The example-test panel works for all modes; in `FullProgram` label the input "קלט (stdin)".

## Work order

1. Domain enum + property + EF config + migration (incl. data-fix SQL).
2. DTOs + validators + `UpdateAssignmentHandler`.
3. Runner: `RunProgramAsync` + `MergeFiles` + usings fix in the existing multi-file path.
4. `AiWorker` switch + multi-file feedback fix.
5. Teacher client: selectButton + conditional display + test hints + models.
6. Student client: per-mode hints/placeholder.

## Verification

1. `dotnet build` in `server/`; `dotnet ef migrations add AddAssignmentGradingMode` (Infrastructure project, Api startup) and review the generated migration.
2. `ng build` in `client/`.
3. Manual E2E (requires Judge0 via docker compose):
   - FullProgram single-file: a complete `Main` program with a loop compiles and passes stdin/stdout tests.
   - FullProgram multi-file: `Person.cs` + `Program.cs`, both starting with `using System;` — no CS1529, the student's `Main` runs.
   - Method assignment: identical behavior to today (regression).
   - Pre-existing multi-file assignment: migration infers `MultiFileMethod`, grading unchanged.
4. Student screen: switching between assignments in the 3 modes changes the hint, placeholder, and input labeling accordingly.
