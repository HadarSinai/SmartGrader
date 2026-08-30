# SmartGrader – Backend Rules (server/)

## Architecture

- **Clean Architecture**: dependency flow is always `Api → Application → Domain`, `Infrastructure → Domain`. Never reference `Infrastructure` from `Application` or `Domain`.
- **CQRS via MediatR**: all business logic lives in handlers. Controllers only call `_mediator.Send(...)`.
- **No business logic in controllers or repositories**.

---

## Naming Conventions

| Type                      | Pattern                                     | Example                        |
| ------------------------- | -------------------------------------------- | ------------------------------- |
| Command                   | `{Verb}{Entity}Command`                     | `CreateLessonCommand`          |
| Query                     | `Get{Entity}Query` / `Get{Entity}ByIdQuery` | `GetLessonsQuery`              |
| Handler                   | `{Command/Query}Handler`                    | `CreateLessonHandler`          |
| Validator                 | `{Command}Validator`                        | `CreateLessonCommandValidator` |
| Request DTO               | `{Verb}{Entity}RequestDto`                  | `CreateLessonRequestDto`       |
| Response DTO              | `{Entity}ResponseDto`                       | `LessonResponseDto`            |
| Repository interface      | `I{Entity}Repository`                       | `ILessonRepository`            |
| Repository implementation | `{Entity}Repository`                        | `LessonRepository`             |
| AutoMapper profile        | `{Entity}Profile`                           | `LessonProfile`                |

---

## Commands & Queries

- Commands and queries are `record` types.
- Each command/query has a corresponding handler class implementing `IRequestHandler<TRequest, TResponse>`.
- Each command has a paired `AbstractValidator<TCommand>` with FluentValidation rules.
- Validation runs automatically via `ValidationBehavior<TRequest, TResponse>` pipeline behavior.

```csharp
// Correct pattern
public record CreateLessonCommand(CreateLessonRequestDto Dto) : IRequest<LessonResponseDto>;

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty();
    }
}

public class CreateLessonHandler : IRequestHandler<CreateLessonCommand, LessonResponseDto>
{
    public async Task<LessonResponseDto> Handle(CreateLessonCommand request, CancellationToken ct)
    {
        var entity = _mapper.Map<Lesson>(request.Dto);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<LessonResponseDto>(entity);
    }
}
```

---

## Domain Entities

- Protected constructors — never instantiate entities with `new` from outside the domain.
- Use static factory methods: `Entity.Create(...)`.
- All entities have a `CreatedAt` property (UTC).
- Use `TestCase` list stored as JSON (`TestsJson` column) on `Assignment`.

---

## Repository Pattern

- Repositories return `IReadOnlyList<T>` for collections.
- Use `AsNoTracking()` for all read queries.
- Most repositories have no `Update` method — use EF change tracking. `StudentRepository` and `UserRepository` do have one, because their read methods return detached entities (`AsNoTracking`) and a handler that mutates one otherwise saves nothing at all.
- Always pass `CancellationToken` to every async method.

```csharp
// Correct repository method signature
Task<IReadOnlyList<Lesson>> GetAllAsync(CancellationToken ct = default);
```

---

## Unit of Work

- Never call `SaveChangesAsync` on the DbContext directly outside of `UnitOfWork`.
- Always call `await _unitOfWork.SaveChangesAsync(ct)` after mutations.

---

## Custom Exceptions → HTTP Mapping

| Exception                   | HTTP Status                  |
| ---------------------------- | ----------------------------- |
| `AppValidationException`    | 400 ValidationProblemDetails |
| `NotFoundException`         | 404 ProblemDetails           |
| `UniqueConstraintException` | 409 ProblemDetails           |
| `BusinessRuleException`     | 400 custom JSON              |

- Throw exceptions from handlers/services, never from controllers.
- `GlobalExceptionMiddleware` handles all mapping (and emails the admin on unhandled errors via `IEmailSender`).

---

## AutoMapper

- One profile class per entity in `Application/Common/Mapping/`.
- Profiles map: `Entity → ResponseDto` and `RequestDto → Entity`.

---

## Dependency Injection

- Application services registered in `Application/DependencyInjection.cs`.
- Infrastructure services registered in `Infrastructure/DependencyInjection.cs`.
- Both called from `Program.cs`: `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(config)`.
- Repositories are `Scoped`. Background services are `Singleton` (queue) + `HostedService`.

---

## Background AI Processing

- Use `IAiJobQueue` to enqueue submissions for AI processing.
- `AiWorker` (BackgroundService) dequeues and calls `IFeedbackService`.
- `Submission.Status` enum: `PendingAi` → `ProcessingAi` → `Done` / `AiFailed`.
- Never call OpenAI directly from a controller or handler — always enqueue.

---

## Excel Import/Export

- Export use cases return `byte[]` through MediatR; controllers wrap them in `File(...)`.
- Import receives a `Stream`, never `IFormFile` — the Application layer must not reference AspNetCore; the controller converts at the boundary.
- Import is partial-success: collect `{ RowNumber, Message }` errors per row and keep going, never roll back all rows for one bad row.
- See [.claude/skills/backend-excel-closedxml-pattern/SKILL.md](../.claude/skills/backend-excel-closedxml-pattern/SKILL.md) for the full pattern.

---

## Database

- SQLite via EF Core.
- Connection string key: `"Default"` in `appsettings.json`.
- Use `dotnet ef migrations add <Name>` to add migrations. Never edit migration files manually.

---

## Secrets

- Never put real credentials (admin password, SMTP credentials, API keys) in `appsettings.json` — it's committed to git. Use `appsettings.Development.json` (gitignored) or user-secrets for real local values; keep `appsettings.json` placeholders empty.
- ⚠️ **A key that has ever been committed is compromised, and removing it from the file does not remove it from the history** — rotate it at the provider. Tracked as open work in [.github/prompts/plan-authOpenWork.prompt.md](../.github/prompts/plan-authOpenWork.prompt.md), along with the `Jwt__Key` environment variable the API will need once it is containerized.

---

## CORS

- Allowed origins come from the `App:AllowedOrigins` configuration array. `appsettings.json` ships it **empty**, which emits no CORS headers at all — the same behaviour as before the policy existed, because in development `client/proxy.conf.json` serves client and API from one origin.
- Real values belong in `appsettings.Development.json` (gitignored) or production environment variables.
- The client authenticates with a `Bearer` header, not cookies, so **do not** enable `AllowCredentials` — and never pair it with `AllowAnyOrigin`.

---
## Business rules live in the registry, not here

Auth throttling, password recovery, account creation, teacher notifications, the analysis engine's
limits and the deletion guards were all written out here as prose. They now have stable ids in
[docs/business-rules.md](../docs/business-rules.md) — **one source of truth, cited by id.** A rule
restated in two places drifts, and the copy is the one that ends up wrong.

| Topic | Rules |
| --- | --- |
| Submissions, attempts, resubmission and locks | `B-1` … `B-10` |
| Login, lockout, password recovery, account creation | `B-11` … `B-24` |
| Teacher notifications and the daily digest | `B-25` … `B-36` |
| What the Roslyn analyzer can and cannot see | `B-37` … `B-45` |
| Test-case defaults, deletion guards, import | `B-46` … `B-50` |

Grading itself — how a number is produced — is [docs/grading-rules.md](../docs/grading-rules.md),
`G-1` … `G-25`.

**One go-live risk still carries an id for a reason:** `B-8` — a submission lock condition specified
and never implemented — needs a human decision before deployment. `B-50` was the second; it is closed
in code, and the startup warning names any account it could not fix.

### Implementation constraints that are not rules

These belong at the code face, not in the registry:

- `app.UseRateLimiter()` must stay **after** `app.UseAuthentication()`. Before it, `httpContext.User`
  carries no claims and the `"ai"` policy — which partitions per user — silently degrades to per-IP.
- The reset-token hash is deliberately **not** `IPasswordHasherService`. That hasher salts randomly, so
  the same token would hash differently on every call and could never be looked up. A 256-bit random
  token has no guessable space, so it needs no slow KDF (`B-19`).
- The digest's Hangfire cron is UTC while its configured hour is local, so the job drifts by an hour
  across a DST change until the next restart. Acceptable for a daily digest; do not copy the pattern
  into anything hour-sensitive (`B-33`).
- One `Submission` row per (student, assignment) is enforced by a unique index, so counting rows in a
  group **is** counting students — no `Distinct` needed (`B-1`).
