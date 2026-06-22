# SmartGrader – Copilot Instructions

This is a full-stack educational grading system (monorepo).

- `server/` — ASP.NET Core Web API (.NET 8), Clean Architecture + CQRS
- `client/` — Angular 17 (standalone components), PrimeNG UI

---

## Repository Structure

```
root/
├── server/          ← C# backend (SmartGrader.sln lives here)
│   ├── Api/         ← Controllers, Middleware, BackgroundServices
│   ├── Application/ ← Use cases (CQRS), DTOs, Services, Validators
│   ├── Domain/      ← Entities, Abstractions (interfaces), no dependencies
│   └── Infrastructure/ ← EF Core, Repositories, External services (OpenAI)
└── client/          ← Angular frontend
    └── src/app/
        ├── core/    ← ApiClient, interceptors
        ├── models/  ← TypeScript interfaces (DTOs)
        ├── pages/   ← Feature components (lessons, students, assignments, submissions)
        └── services/← One service per entity
```

---

## Backend Rules (server/)

### Architecture

- **Clean Architecture**: dependency flow is always `Api → Application → Domain`, `Infrastructure → Domain`. Never reference `Infrastructure` from `Application` or `Domain`.
- **CQRS via MediatR**: all business logic lives in handlers. Controllers only call `_mediator.Send(...)`.
- **No business logic in controllers or repositories**.

### Naming Conventions

| Type                      | Pattern                                     | Example                        |
| ------------------------- | ------------------------------------------- | ------------------------------ |
| Command                   | `{Verb}{Entity}Command`                     | `CreateLessonCommand`          |
| Query                     | `Get{Entity}Query` / `Get{Entity}ByIdQuery` | `GetLessonsQuery`              |
| Handler                   | `{Command/Query}Handler`                    | `CreateLessonHandler`          |
| Validator                 | `{Command}Validator`                        | `CreateLessonCommandValidator` |
| Request DTO               | `{Verb}{Entity}RequestDto`                  | `CreateLessonRequestDto`       |
| Response DTO              | `{Entity}ResponseDto`                       | `LessonResponseDto`            |
| Repository interface      | `I{Entity}Repository`                       | `ILessonRepository`            |
| Repository implementation | `{Entity}Repository`                        | `LessonRepository`             |
| AutoMapper profile        | `{Entity}Profile`                           | `LessonProfile`                |

### Commands & Queries

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

### Domain Entities

- Protected constructors — never instantiate entities with `new` from outside the domain.
- Use static factory methods: `Entity.Create(...)`.
- All entities have a `CreatedAt` property (UTC).
- Use `TestCase` list stored as JSON (`TestsJson` column) on `Assignment`.

### Repository Pattern

- Repositories return `IReadOnlyList<T>` for collections.
- Use `AsNoTracking()` for all read queries.
- Repositories have no `Update` method — use EF change tracking or re-attach.
- Always pass `CancellationToken` to every async method.

```csharp
// Correct repository method signature
Task<IReadOnlyList<Lesson>> GetAllAsync(CancellationToken ct = default);
```

### Unit of Work

- Never call `SaveChangesAsync` on the DbContext directly outside of `UnitOfWork`.
- Always call `await _unitOfWork.SaveChangesAsync(ct)` after mutations.

### Custom Exceptions → HTTP Mapping

| Exception                   | HTTP Status                  |
| --------------------------- | ---------------------------- |
| `AppValidationException`    | 400 ValidationProblemDetails |
| `NotFoundException`         | 404 ProblemDetails           |
| `UniqueConstraintException` | 409 ProblemDetails           |
| `BusinessRuleException`     | 400 custom JSON              |

- Throw exceptions from handlers/services, never from controllers.
- `GlobalExceptionMiddleware` handles all mapping.

### AutoMapper

- One profile class per entity in `Application/Common/Mapping/`.
- Profiles map: `Entity → ResponseDto` and `RequestDto → Entity`.

### Dependency Injection

- Application services registered in `Application/DependencyInjection.cs`.
- Infrastructure services registered in `Infrastructure/DependencyInjection.cs`.
- Both called from `Program.cs`: `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(config)`.
- Repositories are `Scoped`. Background services are `Singleton` (queue) + `HostedService`.

### Background AI Processing

- Use `IAiJobQueue` to enqueue submissions for AI processing.
- `AiWorker` (BackgroundService) dequeues and calls `IFeedbackService`.
- `Submission.Status` enum: `PendingAi` → `ProcessingAi` → `Done` / `AiFailed`.
- Never call OpenAI directly from a controller or handler — always enqueue.

### Database

- SQLite via EF Core.
- Connection string key: `"Default"` in `appsettings.json`.
- Use `dotnet ef migrations add <Name>` to add migrations. Never edit migration files manually.

---

## Frontend Rules (client/)

### Angular Patterns

- All components use `standalone: true` with explicit `imports: [...]` — no `NgModule`.
- Use PrimeNG for all UI: `TableModule`, `ButtonModule`, `CardModule`, `DialogModule`, `InputTextModule`, `CalendarModule`, `TagModule`, `ConfirmDialogModule`, `SkeletonModule`, `TooltipModule`.
- Global toast via `MessageService` (provided in `appConfig`). Import `ToastModule` in root only.

### Models (TypeScript Interfaces)

- All models are `interface` types, never `class`.
- Match backend DTO names exactly.
- Use `string | null` for nullable strings, never `string | undefined`.
- Dates are ISO strings (`string`), not `Date` objects.

```typescript
// Correct model pattern
export interface LessonResponseDto {
  id: number;
  name: string | null;
  lessonDate: string; // ISO 8601
  createdAt: string;
  assignmentsCount: number;
}

export interface CreateLessonRequestDto {
  name: string | null;
  subject: string | null;
  lessonDate: string;
  teacherName: string | null;
}
```

### HTTP Services

- Every entity has its own service in `src/app/services/`.
- Services inject `ApiClient` (not `HttpClient` directly).
- Use `this.api.url('/api/path')` — never hardcode base URLs.
- All methods return `Observable<T>`, never `Promise`.

```typescript
@Injectable({ providedIn: "root" })
export class LessonsService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<LessonResponseDto[]> {
    return this.api.http.get<LessonResponseDto[]>(this.api.url("/api/lessons"));
  }

  create(dto: CreateLessonRequestDto): Observable<LessonResponseDto> {
    return this.api.http.post<LessonResponseDto>(
      this.api.url("/api/lessons"),
      dto,
    );
  }
}
```

### Components

- Inject `MessageService` (from PrimeNG) for all user-facing notifications.
- Use `severity: 'success' | 'error' | 'warn' | 'info'` for toasts.
- Always handle observable errors in `.subscribe({ error: (err) => ... })`.
- Use `ConfirmationService` (PrimeNG) for delete confirmations — never `window.confirm`.
- Navigation via `Router.navigate([...])`, never `location.href`.

### Routing Convention

Nested resources follow this URL pattern:

```
/lessons
/lessons/new
/lessons/:id/edit
/lessons/:lessonId/assignments
/lessons/:lessonId/assignments/new
/lessons/:lessonId/assignments/:id/edit
/students/:studentId/submissions
/students/:studentId/submissions/:submissionId
```

### Error Handling

- `ApiErrorInterceptor` catches HTTP errors globally and maps them to messages.
- Components show errors via `MessageService`, not `console.error` or `alert`.

### TypeScript

- Strict mode enabled — no implicit `any`.
- Always type observables: `Observable<T>`, never `Observable<any>`.
- Use `readonly` for component inputs where applicable.
