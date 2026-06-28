---
description: "Task 2 — Application: Create ICodeRunnerService interface, RunnerResult, and TestCaseResult"
agent: agent
tools: [read_file, create_file, get_errors]
---

# Task 2 — Application: ICodeRunnerService Interface

Follow the rules in [server.instructions.md](../instructions/server.instructions.md).

## Context

The Application layer needs a clean abstraction for code execution so that:

- `AiWorker` depends on an interface, not a concrete implementation
- The implementation (Judge0) lives in Infrastructure and can be swapped

The folder `server/Application/Services/CodeRunner/` currently exists but is **empty**.

---

## Files to create

### 1. `server/Application/Services/CodeRunner/ICodeRunnerService.cs`

```csharp
namespace SmartGrader.Application.Services.CodeRunner;

public interface ICodeRunnerService
{
    Task<RunnerResult> RunAsync(
        string sourceCode,
        string methodName,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default);
}
```

Where `TestCase` is the existing domain entity from `server/Domain/Entities/`.
Read the `Assignment.cs` entity first to confirm the exact `TestCase` type name and namespace.

---

### 2. `server/Application/Services/CodeRunner/RunnerResult.cs`

```csharp
namespace SmartGrader.Application.Services.CodeRunner;

public sealed record RunnerResult(
    int Passed,
    int Total,
    bool HasCompileError,
    string? CompileError,
    IReadOnlyList<TestCaseResult> Details
);
```

---

### 3. `server/Application/Services/CodeRunner/TestCaseResult.cs`

```csharp
namespace SmartGrader.Application.Services.CodeRunner;

public sealed record TestCaseResult(
    string Input,
    string Expected,
    string Actual,
    bool Passed,
    string? Error
);
```

---

## Rules

- These files belong to the **Application layer** — no Infrastructure or EF Core references allowed
- Use `sealed record` for result types (immutable, value semantics)
- Namespace: `SmartGrader.Application.Services.CodeRunner`

---

## Validation

- `dotnet build` from `server/` must pass with no errors
- No circular dependencies introduced
