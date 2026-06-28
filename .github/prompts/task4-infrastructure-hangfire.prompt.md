---
description: "Task 4 — Infrastructure: Replace in-memory Channel queue with Hangfire for persistent background job processing"
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

# Task 4 — Infrastructure: Hangfire (replaces Channel\<int\>)

Follow the rules in [server.instructions.md](../instructions/server.instructions.md).

## Context

The current queue is `Channel<int>` (in-memory RAM). If the server restarts or crashes, all pending submissions are **permanently lost**.

**Hangfire** is a .NET background job library that:

- Stores jobs in the database (SQLite already used in this project)
- Survives server restarts — retries automatically on next boot
- Has a built-in web dashboard at `/hangfire`

### What gets deleted:

- `IAiJobQueue` interface
- `AiJobQueue` implementation
- All usages of `_aiQueue.EnqueueAsync(...)` → replaced with Hangfire

---

## Step 1 — Install NuGet packages

Run from `server/` folder:

```
dotnet add Api/Api.csproj package Hangfire.AspNetCore
dotnet add Infrastructure/Infrastructure.csproj package Hangfire.Core
dotnet add Infrastructure/Infrastructure.csproj package Hangfire.InMemory
```

> Note: Use `Hangfire.InMemory` for now (still persistent enough for our needs).
> When moving to production with SQLite/Postgres, swap to `Hangfire.Storage.SQLite` or `Hangfire.PostgreSql`.

---

## Step 2 — Delete IAiJobQueue and AiJobQueue

Find and **delete** (or fully comment out) these files:

- Wherever `IAiJobQueue.cs` is defined (search `Application/Services/BackgroundJobs/`)
- Wherever `AiJobQueue.cs` is defined (search `Infrastructure/`)

---

## Step 3 — Register Hangfire in Infrastructure DI

File: `server/Infrastructure/DependencyInjection.cs`

Add:

```csharp
using Hangfire;
using Hangfire.InMemory;

// inside AddInfrastructure():
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

services.AddHangfireServer();
```

---

## Step 4 — Add Hangfire dashboard to Program.cs

File: `server/Api/Program.cs`

After `app.UseAuthorization()`, add:

```csharp
app.UseHangfireDashboard("/hangfire");
```

> The dashboard shows all queued, processing, succeeded, and failed jobs.
> In production, add authentication middleware before this line.

---

## Step 5 — Update CreateSubmissionHandler

File: `server/Application/UseCases/Submissions/CreateSubmission/CreateSubmissionHandler.cs`

Replace `IAiJobQueue` injection with `IBackgroundJobClient`:

```csharp
using Hangfire;

// Constructor: inject IBackgroundJobClient _jobClient
// Replace:
await _aiQueue.EnqueueAsync(submission.Id, cancellationToken);

// With:
_jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));
```

Where `IGradeSubmissionJob` is defined in Step 6.

---

## Step 6 — Create IGradeSubmissionJob

File: `server/Application/Services/BackgroundJobs/IGradeSubmissionJob.cs`

```csharp
namespace SmartGrader.Application.Services.BackgroundJobs;

public interface IGradeSubmissionJob
{
    Task ExecuteAsync(int submissionId);
}
```

> The implementation of this interface will be done in Task 5 (AiWorker wiring).
> For now, register a placeholder or leave DI registration for Task 5.

---

## Validation

- `dotnet build` from `server/` must pass
- `IAiJobQueue` no longer referenced anywhere
- App starts and `/hangfire` dashboard is accessible at runtime
- Submitting a new submission does not throw DI errors
