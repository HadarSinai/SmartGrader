---
name: backend-mediatr-query-handler-pattern
description: "Use when adding or reviewing a MediatR Query/Command + Handler in the SmartGrader .NET backend (CQRS use case): new IRequest/IRequestHandler pair under server/Application/UseCases, FluentValidation validators, NotFoundException usage, or AutoMapper calls inside a handler. USE FOR: 'add a query for X', 'create a handler', 'implement GetXByIdQuery', 'add CQRS use case'. NOT for repository query methods (see backend-repository-query-pattern) or controller wiring (see backend-controller-endpoint-pattern)."
---

# Backend MediatR Query/Handler Pattern

SmartGrader uses CQRS via MediatR. Every use case lives in its own folder: `server/Application/UseCases/{Entity}/{UseCaseName}/`, containing a request record, a handler, and optionally a FluentValidation validator. **No manual registration is needed** — both MediatR and FluentValidation auto-discover everything by assembly scan.

## When to Use

- Adding a new read operation, e.g. "get lesson by class name" → `GetLessonsByClassNameQuery` + handler.
- Adding a new use case folder for an existing entity, following the same shape as `GetLessonById`.
- Reviewing a handler for correct `NotFoundException` usage, mapping, or ownership checks.

## Workflow

1. **Create the folder**: `server/Application/UseCases/{Entity}/{UseCaseName}/` (entity folder name is usually plural, e.g. `Submissions`, `Lessons`).
2. **Add `{Name}Query.cs`** (or `{Name}Command.cs` for writes) as a `record`:
   ```csharp
   public record {Name}Query(int Id) : IRequest<{Entity}ResponseDto>;
   ```
3. **Add `{Name}Handler.cs`** implementing `IRequestHandler<TQuery, TDto>`:
   - Inject the repository interface + `IMapper` via constructor (commands also inject `IUnitOfWork`).
   - Call the repository method (see [backend-repository-query-pattern](../backend-repository-query-pattern/SKILL.md) if the method doesn't exist yet).
   - `throw new NotFoundException(nameof(Entity), id)` when the entity is `null`.
   - `return _mapper.Map<TDto>(entity)`.
4. **Optionally add `{Name}Validator.cs`**: `public class {Name}Validator : AbstractValidator<{Name}Query>` with rules like `RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");`. The `ValidationBehavior<TRequest,TResponse>` pipeline behavior runs it automatically before the handler.
5. **No DI wiring needed** — `services.AddMediatR(...)` and `services.AddValidatorsFromAssembly(...)` in `server/Application/DependencyInjection.cs` scan the whole assembly.

## Real Examples

Simple case — [`GetLessonByIdHandler`](../../../server/Application/UseCases/Lessons/GetLessonById/GetLessonByIdHandler.cs):

```csharp
public class GetLessonByIdHandler : IRequestHandler<GetLessonByIdQuery, LessonResponseDto>
{
    private readonly ILessonRepository _repository;
    private readonly IMapper _mapper;

    public GetLessonByIdHandler(ILessonRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<LessonResponseDto> Handle(GetLessonByIdQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (lesson is null)
            throw new NotFoundException("Lesson", request.Id);
        return _mapper.Map<LessonResponseDto>(lesson);
    }
}
```

Composite-key + ownership check — [`GetSubmissionByIdHandler`](../../../server/Application/UseCases/Submission/GetSubmissionById/GetSubmissionByIdHandler.cs):

```csharp
public record GetSubmissionByIdQuery(int StudentId, int SubmissionId) : IRequest<SubmissionResponseDto>;

// ...in the handler:
var submission = await _repository.GetByIdAsync(request.SubmissionId, cancellationToken);

if (submission is null)
    throw new NotFoundException(nameof(Submission), request.SubmissionId);

// ownership check — submission must belong to the requesting student
if (submission.StudentId != request.StudentId)
    throw new NotFoundException("Submission does not belong to this student.", request.SubmissionId);

return _mapper.Map<SubmissionResponseDto>(submission);
```

Matching validator — `GetSubmissionByIdValidator`:

```csharp
public class GetSubmissionByIdValidator : AbstractValidator<GetSubmissionByIdQuery>
{
    public GetSubmissionByIdValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Id must be greater than 0.");
        RuleFor(x => x.SubmissionId).GreaterThan(0).WithMessage("Id must be greater than 0.");
    }
}
```

## Template

Copy-paste starting point: [assets/query-handler.template.cs](./assets/query-handler.template.cs)

## Pitfalls

- Don't skip the null check — every handler throws `NotFoundException` before mapping, it's never silently returned as `null`.
- Don't try/catch inside the handler — `GlobalExceptionMiddleware` centrally maps `NotFoundException` → 404 and validation errors → 400.
- Don't manually register the query/handler/validator anywhere — assembly scanning in `server/Application/DependencyInjection.cs` finds them automatically as long as they're in the `Application` assembly.
- For nested/owned resources (e.g. a submission that belongs to a student), always include the parent id in the query and verify ownership in the handler, not just existence.

## See Also

- [backend-repository-query-pattern](../backend-repository-query-pattern/SKILL.md) — the repository method this handler calls.
- [backend-automapper-profile-pattern](../backend-automapper-profile-pattern/SKILL.md) — the profile behind `_mapper.Map<TDto>(entity)`.
- [backend-controller-endpoint-pattern](../backend-controller-endpoint-pattern/SKILL.md) — the controller action that sends this query.
