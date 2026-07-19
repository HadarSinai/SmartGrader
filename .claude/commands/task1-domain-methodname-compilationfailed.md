---
description: "Task 1 — Domain: Add MethodName to Assignment and CompilationFailed status to Submission"
---

# Task 1 — Domain: MethodName + CompilationFailed

Follow the rules in [server.instructions.md](../../.github/instructions/server.instructions.md).

## Context

The 3-step grading pipeline (Judge0 → AI) requires:

1. `Assignment.MethodName` — so Judge0 knows which method to test
2. `SubmissionStatus.CompilationFailed` — a dedicated status when student code doesn't compile
3. `Submission.CompileError` — field to store the compiler error message

---

## Step 1 — Assignment entity

File: `server/Domain/Entities/Assignment.cs`

Add a `MethodName` property:

```csharp
public string MethodName { get; private set; } = "";
```

Update the constructor to accept and assign `methodName`.

---

## Step 2 — Submission entity

File: `server/Domain/Entities/Submission.cs`

### 2a — Add CompilationFailed to enum

```csharp
public enum SubmissionStatus
{
    PendingAi       = 0,
    ProcessingAi    = 1,
    Done            = 2,
    AiFailed        = 3,
    CompilationFailed = 4
}
```

### 2b — Add CompileError field

```csharp
public string? CompileError { get; private set; }
```

### 2c — Add domain method (same pattern as MarkAiFailed)

```csharp
public void MarkCompilationFailed(string error)
{
    // Valid from PendingAi or ProcessingAi
    Status = SubmissionStatus.CompilationFailed;
    CompileError = error;
}
```

---

## Step 3 — DTOs

### CreateAssignmentRequestDto

File: `server/Application/Dtos/Assignments/CreateAssignmentRequestDto.cs`

Add:

```csharp
public string MethodName { get; init; } = "";
```

### UpdateAssignmentRequestDto

File: `server/Application/Dtos/Assignments/UpdateAssignmentRequestDto.cs`

Add:

```csharp
public string? MethodName { get; init; }
```

### AssignmentResponseDto

File: `server/Application/Dtos/Assignments/AssignmentResponseDto.cs`

Add:

```csharp
public string MethodName { get; init; } = "";
```

### SubmissionResponseDto

File: `server/Application/Dtos/Submissions/SubmissionResponseDto.cs`

Add:

```csharp
public string? CompileError { get; init; }
```

---

## Step 4 — AutoMapper profiles

Update `AssignmentProfile` to map `MethodName`.
Update `SubmissionProfile` to map `CompileError`.

---

## Step 5 — EF Migration

Run in terminal from `server/` folder:

```
dotnet ef migrations add AddMethodNameAndCompilationFailed --project Infrastructure --startup-project Api
dotnet ef database update --project Infrastructure --startup-project Api
```

---

## Validation

- Build must pass with no errors: `dotnet build`
- Migration file created under `server/Infrastructure/Migrations/`
- `MethodName` appears in `Assignments` table
- `CompileError` appears in `Submissions` table
