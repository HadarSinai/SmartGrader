---
description: "Task 5 — Api: Wire AiWorker to call Judge0 (real tests) then OpenAI (real score) — connects the full 3-step grading pipeline"
agent: agent
tools:
  [
    read_file,
    replace_string_in_file,
    multi_replace_string_in_file,
    create_file,
    get_errors,
  ]
---

# Task 5 — Api: Wire AiWorker — Full 3-Step Pipeline

Follow the rules in [server.instructions.md](../instructions/server.instructions.md).

**Depends on:** Task 1 (MethodName, CompilationFailed), Task 2 (ICodeRunnerService), Task 3 (Judge0CodeRunner), Task 4 (Hangfire / IGradeSubmissionJob)

## Context

`AiWorker` currently hardcodes `passedTests = 0, totalTests = 0` and never calls any code runner.

In this task, `AiWorker` is **refactored** to implement `IGradeSubmissionJob` and wire all 3 steps:

1. **Judge0** → compile + run tests → real results
2. **CompilationFailed** → if compile error, mark and stop
3. **OpenAI** → use real passed/total counts → real score

Read the current `server/Api/BackgroundServices/AiWorker.cs` **before** making any changes.

---

## Step 1 — AiWorker implements IGradeSubmissionJob

File: `server/Api/BackgroundServices/AiWorker.cs`

`AiWorker` should implement `IGradeSubmissionJob` from `SmartGrader.Application.Services.BackgroundJobs`.

Add `ICodeRunnerService` to constructor injection (alongside existing `IFeedbackService`).

---

## Step 2 — Load Assignment with Tests

When fetching the submission, ensure `Assignment` (with its `Tests` and `MethodName`) is loaded.

Check `ISubmissionRepository.GetByIdAsync` — if it does not include `Assignment`, update the repository query to use `.Include(s => s.Assignment)`.

File to check: `server/Infrastructure/Repositories/SubmissionRepository.cs`

---

## Step 3 — Replace hardcoded 0/0 with Judge0 call

Replace:

```csharp
// TODO: בהמשך להחליף ל-results אמיתיים מטסטים
int passedTests = 0, totalTests = 0;
```

With:

```csharp
var runnerResult = await _codeRunner.RunAsync(
    submission.SourceCode,
    submission.Assignment!.MethodName,
    submission.Assignment.Tests,
    stoppingToken);
```

---

## Step 4 — Handle CompilationFailed

After getting `runnerResult`, **before** calling OpenAI:

```csharp
if (runnerResult.HasCompileError)
{
    submission.MarkCompilationFailed(runnerResult.CompileError ?? "Unknown compile error");
    await uow.SaveChangesAsync(stoppingToken);
    continue; // skip OpenAI
}
```

---

## Step 5 — Pass real counts to OpenAI

Replace:

```csharp
var aiJson = await feedback.GetFeedbackAsync(
    assignmentDescription,
    submission.SourceCode,
    passedTests,
    totalTests,
    stoppingToken);
```

With:

```csharp
var aiJson = await feedback.GetFeedbackAsync(
    assignmentDescription,
    submission.SourceCode,
    runnerResult.Passed,
    runnerResult.Total,
    stoppingToken);
```

---

## Step 6 — Fix score calculation

The score line already exists but used 0/0:

```csharp
score: totalTests > 0 ? (double)passedTests / totalTests * 100 : 0,
```

Update to use `runnerResult.Passed` and `runnerResult.Total`.

---

## Step 7 — Register IGradeSubmissionJob in DI

File: `server/Infrastructure/DependencyInjection.cs` or `server/Api/Program.cs`

```csharp
services.AddScoped<IGradeSubmissionJob, AiWorker>();
```

> `AiWorker` is the concrete implementation of `IGradeSubmissionJob`. It lives in the Api layer but is registered so Hangfire can resolve it.

---

## Full flow after this task

```
Hangfire dequeues submissionId
        ↓
IGradeSubmissionJob.ExecuteAsync(id)  ← AiWorker
        ↓
Step 1: Judge0.RunAsync(code, methodName, tests)
        ↓ HasCompileError?
       Yes → MarkCompilationFailed → SaveChanges → done
        ↓ No
Step 2: OpenAI.GetFeedbackAsync(description, code, passed, total)
        ↓
Step 3: MarkDone(score, aiJson) → SaveChanges
```

---

## Validation

- `dotnet build` from `server/` must pass
- Submit code with a syntax error → `Status = CompilationFailed`, `CompileError` is populated
- Submit valid code → `Status = Done`, `Score` reflects real test results
- `passedTests = 0, totalTests = 0` TODO comment is fully removed

---

## Implementation Log

**Date:** 2026-06-28  
**Status:** ✅ Completed — `dotnet build` passed (4/4 projects succeeded, 0 errors, 0 warnings)

### Changes Made

| File                                        | Change                                                                                        |
| ------------------------------------------- | --------------------------------------------------------------------------------------------- |
| `server/Api/BackgroundServices/AiWorker.cs` | Replaced all commented-out legacy code with new `IGradeSubmissionJob` implementation          |
| `server/Api/Program.cs`                     | Added `using` statements and registered `services.AddScoped<IGradeSubmissionJob, AiWorker>()` |

### Notes

- `SubmissionRepository.GetByIdAsync` already included `.Include(s => s.Assignment)` — **no change needed**.
- `Assignment.Tests` is a `[NotMapped]` computed property that deserializes from `TestsJson` — no extra EF include required.
- `AiWorker` is `Scoped` (not Singleton) because it depends on scoped EF Core services (`ISubmissionRepository`, `IUnitOfWork`).
- Hangfire resolves `IGradeSubmissionJob` per-job invocation via the DI container, so `AddScoped` is correct.

### Build Output

```
Domain     net8.0  succeeded
Application net8.0 succeeded
Infrastructure net8.0 succeeded
Api        net8.0  succeeded (1.5s total)
```
