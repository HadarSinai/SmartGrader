# Plan: Connect 3-Step Submission Grading Pipeline

## Status: ✅ Completed (executed via task1–task7 prompt files, with architecture revisions — see Decisions)

## Summary

The 3-step grading pipeline (Compile → Test → AI) is partially built but disconnected. The Runner project exists as a standalone Roslyn console app but is never called. AiWorker hardcodes passedTests=0, totalTests=0. Assignment has no MethodName field. ICodeRunnerService interface and implementation folders are empty.

## Architecture Findings

**What EXISTS:**

- `Runner` project — standalone console app: reads JSON from stdin, compiles with Roslyn, runs tests by reflection, returns JSON to stdout
- `AiWorker` — background service dequeuing submission IDs, calls AI, but passedTests/totalTests are HARDCODED to 0/0 with a TODO comment
- `IFeedbackService` / `OpenAiFeedbackService` — fully working AI step
- `IAiJobQueue` / `AiJobQueue` — Channel<int> queue, works
- `SubmissionStatus` enum: PendingAi→ProcessingAi→Done/AiFailed
- `Assignment.Tests` (List<TestCase> stored as TestsJson) — exists

**What is MISSING (gaps to fill):**

1. `Assignment.MethodName` — Runner needs to know which method to call, but the field doesn't exist on the entity
2. `ICodeRunnerService` interface — folder `Application/Services/CodeRunner/` is EMPTY
3. `LocalProcessCodeRunner` implementation — folder `Infrastructure/Services/CodeRunner/` is EMPTY; commented out in DI
4. `AiWorker` doesn't call Runner — hardcoded 0/0 with TODO
5. `SubmissionStatus.CompilationFailed` — no status for compile failure (currently would go to AiFailed)
6. `Submission.CompileError` — no separate field for compile errors (only `AiError`)
7. Runner path configuration in appsettings

## Plan

### Phase 1: Domain — Add MethodName to Assignment ✅ Done

- Add `MethodName` string property to `Assignment` entity
- Add `MethodName` to `CreateAssignmentRequestDto` and `UpdateAssignmentRequestDto`
- Add EF migration

### Phase 2: Domain — Add CompilationFailed status ✅ Done

- Add `CompilationFailed = 4` to `SubmissionStatus` enum
- Add `MarkCompilationFailed(string error)` method on `Submission` entity (from PendingAi or ProcessingAi)
- Add EF migration for enum change (if stored as int, no migration needed)

### Phase 3: Application — ICodeRunnerService interface ✅ Done

- Create `Application/Services/CodeRunner/ICodeRunnerService.cs`
- Create `Application/Services/CodeRunner/RunnerResult.cs` (Passed, Total, HasCompileError, CompileError, Details)
- Create `Application/Services/CodeRunner/TestCaseResult.cs` (Input, Expected, Actual, Passed, Error)

### Phase 4: Infrastructure — Code Runner ✅ Done (revised: Judge0CodeRunner, not LocalProcessCodeRunner)

- ~~Create `Infrastructure/Services/CodeRunner/LocalProcessCodeRunner.cs`~~ Superseded — implemented `Judge0CodeRunner.cs` instead, calling the Judge0 HTTP API (sandboxed Docker execution) rather than spawning the local `Runner.exe` process
- Maps response to `RunnerResult`
- Registered `ICodeRunnerService → Judge0CodeRunner` in `Infrastructure/DependencyInjection.cs` via `AddHttpClient<ICodeRunnerService, Judge0CodeRunner>()`
- Added `Judge0Options` config class + `appsettings.json` / `Judge0` config section
- Judge0 sandbox itself provisioned via `docker-compose.yml` (see task7)

### Phase 5: Api — Update AiWorker ✅ Done (revised: Hangfire job, not Channel queue)

- Inject `ICodeRunnerService` into `AiWorker`
- Step 1: Call `RunAsync(sourceCode, assignment.MethodName, assignment.Tests)`
- Step 2: If `HasCompileError` → `MarkCompilationFailed(error)` → SaveChanges → continue
- Step 3: Use real `passedTests`/`totalTests` in `GetFeedbackAsync`
- Step 4: `MarkDone(score, aiJson)` as before
- `AiWorker` now implements `IGradeSubmissionJob` and is dispatched via Hangfire (`IBackgroundJobClient`) instead of the in-memory `Channel<int>` (`IAiJobQueue`), so pending submissions survive server restarts

## Relevant Files

- server/Domain/Entities/Assignment.cs — add MethodName
- server/Domain/Entities/Submission.cs — add MarkCompilationFailed + status
- server/Application/Dtos/Assignments/ — add MethodName to request/response DTOs
- server/Application/Services/CodeRunner/ — create interface + result types
- server/Infrastructure/Services/CodeRunner/ — create implementation
- server/Infrastructure/DependencyInjection.cs — register ICodeRunnerService
- server/Api/BackgroundServices/AiWorker.cs — wire all 3 steps
- server/Api/appsettings.json — add Runner:ExePath
- server/Runner/Program.cs — already complete, no changes needed

## Verification

1. Submit code with compile error → Status becomes CompilationFailed
2. Submit valid code with failing tests → passedTests < totalTests sent to AI
3. Submit valid code passing all tests → passedTests = totalTests sent to AI
4. AI response stored in Comments

## Decisions

- CompilationFailed added as new status (semantically cleaner than AiFailed)
- MethodName added to Assignment (required for the code runner to know which method to invoke)
- **Revised during implementation:** the standalone `Runner` Roslyn console app was dropped in favor of **Judge0** (external sandboxed execution service, run via Docker Compose) — safer isolation than an in-process/local child process, and no longer requires maintaining the Runner project. `server/Runner/Program.cs` is now unused/dead code.
- **Revised during implementation:** the in-memory `Channel<int>` queue (`IAiJobQueue`/`AiJobQueue`) was replaced with **Hangfire** (`IGradeSubmissionJob`, DB-persisted jobs) so queued submissions aren't lost on server restart.
- Actual implementation was carried out via the task1–task7 prompt files (`.github/prompts/task1-domain-methodname-compilationfailed.prompt.md` through `task7-infrastructure-docker-compose.prompt.md`), which superseded this plan's phase breakdown.
