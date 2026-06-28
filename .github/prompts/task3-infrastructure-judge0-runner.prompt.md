---
description: "Task 3 — Infrastructure: Implement Judge0CodeRunner to replace Runner.exe with Docker-sandboxed code execution"
agent: agent
tools:
  [
    read_file,
    create_file,
    replace_string_in_file,
    multi_replace_string_in_file,
    run_in_terminal,
    get_errors,
  ]
---

# Task 3 — Infrastructure: Judge0CodeRunner

Follow the rules in [server.instructions.md](../instructions/server.instructions.md).

**Depends on:** Task 2 (ICodeRunnerService must exist)

## Context

The existing `server/Runner/` project runs student code unsafely (Assembly.Load into same process, no resource limits, full filesystem access). It is **not suitable for production**.

**Judge0** is an open-source code execution system that runs each submission inside an isolated Docker container with:

- CPU/memory limits
- No network access
- No filesystem access to host
- Configurable time limit

The `server/Infrastructure/Services/CodeRunner/` folder is **empty** and ready.

Judge0 API reference:

- `POST /submissions?wait=true` — submit code, wait for result
- Response fields: `status.id`, `stdout`, `stderr`, `compile_output`, `time`, `memory`
- Status IDs: 3=Accepted, 4=Wrong Answer, 5=Time Limit Exceeded, 6=Compilation Error, 11=Runtime Error

---

## Step 1 — Install NuGet (no new packages needed)

`System.Net.Http` is already available via ASP.NET Core. No extra NuGet required.

---

## Step 2 — `server/Infrastructure/Services/CodeRunner/Judge0Options.cs`

```csharp
namespace SmartGrader.Infrastructure.Services.CodeRunner;

public sealed class Judge0Options
{
    public string BaseUrl { get; init; } = "http://localhost:2358";
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
    public int LanguageId { get; init; } = 51; // 51 = C# (Mono)
}
```

---

## Step 3 — `server/Infrastructure/Services/CodeRunner/Judge0CodeRunner.cs`

Implement `ICodeRunnerService` from `SmartGrader.Application.Services.CodeRunner`.

### Logic per submission:

For each `TestCase` in `tests`:

1. Build the full C# source by wrapping student code in `StudentSolution` class (same pattern as old Runner):

```csharp
var wrappedSource = $@"
using System;
using System.Linq;
public static class StudentSolution
{{
    {sourceCode}
}}
public class Program
{{
    public static void Main(string[] args)
    {{
        var parts = Console.ReadLine()!.Split(' ');
        var result = StudentSolution.{methodName}({BuildArgs(method parameters)});
        Console.WriteLine(result);
    }}
}}";
```

2. POST to `{BaseUrl}/submissions?wait=true` with:
   - `source_code` = wrappedSource
   - `language_id` = options.LanguageId
   - `stdin` = testCase.Input
   - `expected_output` = testCase.Expected
   - `cpu_time_limit` = options.TimeoutSeconds
3. Parse response JSON

### Response mapping:

- `status.id == 6` → `HasCompileError = true`, return immediately (no need to run remaining tests)
- `status.id == 5` → treat as failed test with error "Time Limit Exceeded"
- `status.id == 3` → Passed
- anything else → Failed

### Return `RunnerResult`:

```csharp
return new RunnerResult(
    Passed: passed,
    Total: tests.Count,
    HasCompileError: false,
    CompileError: null,
    Details: details
);
```

### HTTP: use `IHttpClientFactory` (injected via constructor). Set `Authorization: Token {ApiKey}` header only if `ApiKey` is not null.

---

## Step 4 — Register in DI

File: `server/Infrastructure/DependencyInjection.cs`

Add:

```csharp
services.AddHttpClient<ICodeRunnerService, Judge0CodeRunner>();
services.Configure<Judge0Options>(configuration.GetSection("Judge0"));
```

Also fix the existing duplicate registration — remove the second `services.AddHttpClient<IFeedbackService, OpenAiFeedbackService>()` line (it appears twice).

Required using:

```csharp
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Infrastructure.Services.CodeRunner;
```

---

## Step 5 — appsettings.json

File: `server/Api/appsettings.json`

Add section:

```json
"Judge0": {
  "BaseUrl": "http://localhost:2358",
  "TimeoutSeconds": 10,
  "LanguageId": 51
}
```

File: `server/Api/appsettings.Development.json`

Add same section (BaseUrl points to local Judge0 Docker instance).

---

## Step 6 — DO NOT delete Runner project yet

Keep `server/Runner/` as-is. It is no longer called by anything but do not delete the files in this task.

---

## Validation

- `dotnet build` from `server/` must pass
- `Judge0CodeRunner` implements `ICodeRunnerService` — no compilation errors
- DI duplicate removed — only one `AddHttpClient<IFeedbackService>` remains
