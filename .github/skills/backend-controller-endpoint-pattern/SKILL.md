---
name: backend-controller-endpoint-pattern
description: "Use when adding or reviewing an ASP.NET Core Controller endpoint in the SmartGrader backend: new [HttpGet]/[HttpPost]/[HttpPut]/[HttpDelete] actions, IMediator wiring, route templates (including nested resources like {parentId:int}/children/{childId:int}), or CreatedAtAction responses. USE FOR: 'add an endpoint', 'add a controller action', 'expose a new API route', 'wire up a query/command to the API'. NOT for the query/handler itself (see backend-mediatr-query-handler-pattern) or repository methods (see backend-repository-query-pattern)."
---

# Backend Controller Endpoint Pattern

Controllers live in `server/Api/Controllers/*.cs`. They are thin: `[ApiController][Route("api/[controller]")]`, constructor-inject `IMediator` (and `IMapper` only if the controller itself needs to map, which is rare), and every action just builds a Query/Command, sends it through MediatR, and returns the result. **No try/catch** — `server/Api/Middlewares/GlobalExceptionMiddleware.cs` centrally maps `NotFoundException` → 404 and `AppValidationException` → 400.

## When to Use

- Exposing a new query/command (built via [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md)) as an HTTP endpoint.
- Adding a nested-resource route, e.g. assignments under a lesson, or submissions under a student.
- Reviewing an existing action for correct verb/route/response-type conventions.

## Workflow

1. **Pick the controller** under `server/Api/Controllers/` (one per top-level resource), or scaffold a new one with `[ApiController][Route("api/[controller]")]` and constructor-injected `IMediator`.
2. **Add the action** with the matching `[Http*]` attribute and route template:
   - Flat resource: `[HttpGet("{id:int}")]`.
   - Nested resource: `[HttpGet("{parentId:int}/children/{childId:int}")]` (mirrors `LessonsController`'s `{lessonId:int}/assignments/{assignmentId:int}`).
3. **Always add a trailing `CancellationToken cancellationToken` parameter**, and `[FromBody] dto` for POST/PUT bodies.
4. **Build the Query/Command and send it**: `await _mediator.Send(new XQuery(...), cancellationToken)`.
5. **Return the right result per verb**:
   - GET → `Ok(result)`.
   - POST → `CreatedAtAction(nameof(GetById), new { id = created.Id }, created)` — the id in the route values **must come from the response DTO returned by the handler** (`created.Id`), never from the request DTO.
   - PUT → `Ok(updated)`.
   - DELETE → `NoContent()`.
6. **Never try/catch** in the action — rely on `GlobalExceptionMiddleware`.

### DI convention (explicit pitfall)

Always constructor-inject `IMediator` as `private readonly IMediator _mediator;`, matching `LessonsController`/`StudentsController`. Do **not** use `[FromServices]` at the method level even though it's valid in .NET 7+ — the codebase has zero precedent for it and it breaks consistency.

### `CreatedAtAction` id extraction (explicit workflow step)

For POST actions, the id passed to `CreatedAtAction`'s route-values must come from the **response DTO returned by the handler**, never the request DTO. For nested resources, include the parent route param too:

```csharp
return CreatedAtAction(
    nameof(GetAssignmentById),
    new { lessonId = lessonId, assignmentId = created.Id },
    created);
```

## Real Examples

Flat resource — [`StudentsController`](../../../server/Api/Controllers/StudentsController.cs):

```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
{
    StudentResponseDto student = await _mediator.Send(new GetStudentByIdQuery(id), cancellationToken);
    return Ok(student);
}

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateStudentRequestDto dto, CancellationToken cancellationToken)
{
    StudentResponseDto created = await _mediator.Send(new CreateStudentCommand(dto), cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
}
```

Nested resource, two-level route — [`LessonsController.CreateAssignment`](../../../server/Api/Controllers/LessonsController.cs):

```csharp
[HttpGet("{lessonId:int}/assignments/{assignmentId:int}")]
public async Task<IActionResult> GetAssignmentById(int lessonId, int assignmentId, CancellationToken cancellationToken)
{
    AssignmentResponseDto result = await _mediator.Send(
        new GetAssignmentByIdQuery(lessonId, assignmentId), cancellationToken);
    return Ok(result);
}

[HttpPost("{lessonId:int}/assignments")]
public async Task<IActionResult> CreateAssignment(
    int lessonId, [FromBody] CreateAssignmentRequestDto dto, CancellationToken cancellationToken)
{
    AssignmentResponseDto created = await _mediator.Send(
        new CreateAssignmentCommand(lessonId, dto), cancellationToken);

    return CreatedAtAction(
        nameof(GetAssignmentById),
        new { lessonId = lessonId, assignmentId = created.Id },
        created);
}
```

Nested resource under students — [`StudentsController.CreateSubmission`](../../../server/Api/Controllers/StudentsController.cs):

```csharp
[HttpPost("{studentId:int}/submissions")]
public async Task<IActionResult> CreateSubmission(
    int studentId, [FromBody] CreateSubmissionRequestDto dto, CancellationToken cancellationToken)
{
    SubmissionResponseDto created = await _mediator.Send(
        new CreateSubmissionCommand(studentId, dto), cancellationToken);

    return CreatedAtAction(
        nameof(GetSubmissionById),
        new { studentId = studentId, submissionId = created.Id },
        created);
}
```

## Template

Copy-paste starting point (flat + nested-resource variants): [assets/controller-endpoint.template.cs](./assets/controller-endpoint.template.cs)

## Pitfalls

- Don't use `[FromServices]` method-level injection — always constructor-inject `IMediator`.
- Don't build `CreatedAtAction` route values from the request DTO — always use the handler's response DTO (`created.Id`).
- Don't add try/catch blocks — `GlobalExceptionMiddleware` handles `NotFoundException`/`AppValidationException` centrally.
- Don't forget the parent route param in nested-resource `CreatedAtAction` calls.
- Don't drop the trailing `CancellationToken cancellationToken` parameter — every existing action takes it and threads it into `_mediator.Send`.

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the Query/Command this action sends.
- [backend-repository-query-pattern](../backend-repository-query-pattern/SKILL.md) and [backend-automapper-profile-pattern](../backend-automapper-profile-pattern/SKILL.md) — the layers underneath the handler.
