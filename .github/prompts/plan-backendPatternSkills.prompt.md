# Plan: Backend CQRS/EF Core Skills (4 skills)

Create 4 project-scoped Skills under `.github/skills/` that teach the repo's real backend patterns (Repository query methods, MediatR Query+Handler, AutoMapper profiles, Controller endpoints), each backed by a real example from the codebase and a copy-paste template asset.

**Location convention**: `.github/skills/<name>/SKILL.md` (+ `assets/*.template.cs`), matching existing `.github/skills/create-skill/`.

## Research findings (already gathered, no more discovery needed)

- Repositories: `server/Infrastructure/Repositories/*.cs` implement interfaces in `server/Domain/Abstractions/I*.cs`. Constructor injects `GradeSheetContext _context`. Pattern: `Task<IReadOnlyList<T>> GetByXAsync(int x, CancellationToken ct = default) => _context.{Set}.Where(...).Include(...).AsNoTracking().ToListAsync(ct);` single-entity variants use `FirstOrDefaultAsync(predicate, ct)` and return `T?`. No generic base repo. No `SaveChangesAsync` in repo (that's `IUnitOfWork`, only used by commands). Example: `SubmissionRepository.GetByStudentIdAsync`. DI: `services.AddScoped<IFooRepository, FooRepository>()` in `server/Infrastructure/DependencyInjection.cs`.
- MediatR: one folder per use case under `server/Application/UseCases/{Entity}/{UseCaseName}/` containing `{Name}Query.cs` (or `Command.cs`), `{Name}Handler.cs`, optional `{Name}Validator.cs` (FluentValidation `AbstractValidator<T>`). Query = `public record XByIdQuery(int Id) : IRequest<XResponseDto>;`. Handler implements `IRequestHandler<TQuery,TResult>`, injects repo + `IMapper` (+ `IUnitOfWork` for commands), throws `SmartGrader.Application.Common.Exceptions.NotFoundException(name, key)` when null, maps via `_mapper.Map<TDto>(entity)`. No manual registration needed — `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))` + `AddValidatorsFromAssembly` in `server/Application/DependencyInjection.cs` auto-discovers everything. `ValidationBehavior<TRequest,TResponse>` pipeline behavior auto-runs validators. Example: `GetLessonByIdHandler`, `GetSubmissionByIdHandler` (composite key + ownership check).
- AutoMapper: profiles in `server/Application/Common/Mapping/{Entity}Profile.cs`, class extends `Profile`, parameterless ctor, `CreateMap<Entity, ResponseDto>().ForMember(d => d.Computed, opt => opt.MapFrom(s => ...))`, `CreateMap<CreateRequestDto, Entity>()`, `CreateMap<UpdateRequestDto, Entity>().ForMember(d => d.Id, opt => opt.Ignore())...`, `.ReverseMap()` for symmetric DTO<->value-object mapping (e.g. `TestCaseDto`<->`TestCase`). Auto-discovered via `services.AddAutoMapper(assembly)` — no manual registration. Note: `AssignmentProfile.cs` incorrectly sits in namespace `SmartGrader.Api.Mapping` (inconsistent) — new profiles should use `SmartGrader.Application.Common.Mapping`.
- Controllers: `server/Api/Controllers/*.cs`, `[ApiController] [Route("api/[controller]")]`, constructor injects `IMediator` (+ `IMapper` only if controller itself maps). Actions: `[HttpGet]`, `[HttpGet("{id:int}")]`, `[HttpPost]` with `[FromBody] dto`, `[HttpPut("{id:int}")]`, `[HttpDelete("{id:int}")]`; all take trailing `CancellationToken cancellationToken` and call `await _mediator.Send(new XQuery(...), cancellationToken)`. Returns: `Ok(result)`, `CreatedAtAction(nameof(GetById), new { id = created.Id }, created)`, `NoContent()`. No try/catch — `GlobalExceptionMiddleware` (server/Api/Middlewares/GlobalExceptionMiddleware.cs) maps `NotFoundException`→404, `AppValidationException`→400 centrally. Nested-resource routes supported, e.g. `[HttpGet("{lessonId:int}/assignments/{assignmentId:int}")]` in `LessonsController`.

## Steps

**Phase 1 — Scaffold skill folders (can run in parallel, independent)**

1. Create `.github/skills/backend-repository-query-pattern/SKILL.md` + `assets/repository-query.template.cs`
2. Create `.github/skills/backend-mediatr-query-handler-pattern/SKILL.md` + `assets/query-handler.template.cs`
3. Create `.github/skills/backend-automapper-profile-pattern/SKILL.md` + `assets/profile.template.cs`
4. Create `.github/skills/backend-controller-endpoint-pattern/SKILL.md` + `assets/controller-endpoint.template.cs`

Each SKILL.md follows the create-skill format (frontmatter `name` matches folder, keyword-rich `description` in Hebrew+English incl. "when to use"/"when not to use", body: When to Use (3+ examples) → Workflow (numbered steps referencing real files) → Template (link to asset) → Pitfalls). Keep under 500 lines; heavy boilerplate goes in the `.cs` template asset, not inline.

### Content per skill

1. **backend-repository-query-pattern**
   - description keywords: Repository, GetBy...Async, EF Core, Where/Include/AsNoTracking, Domain/Abstractions interface, Infrastructure/Repositories
   - Workflow: (a) add signature to `I{Entity}Repository` in `server/Domain/Abstractions/`, (b) implement in `server/Infrastructure/Repositories/{Entity}Repository.cs` following `SubmissionRepository.GetByStudentIdAsync` shape, (c) choose return type (`IReadOnlyList<T>` vs `T?`) and matching EF call (`ToListAsync` vs `FirstOrDefaultAsync`), (d) always thread `CancellationToken ct = default`, (e) register new repo in `server/Infrastructure/DependencyInjection.cs` only if the interface/class itself is new.
   - Pitfall note: don't call `SaveChangesAsync` inside repo methods (queries are read-only; persistence goes through `IUnitOfWork` in command handlers).

2. **backend-mediatr-query-handler-pattern**
   - description keywords: CQRS, MediatR, IRequest, IRequestHandler, Query, Handler, FluentValidation, Application/UseCases
   - Workflow: (a) create folder `server/Application/UseCases/{Entity}/{UseCaseName}/`, (b) add `{Name}Query.cs` as `public record ... : IRequest<TDto>`, (c) add `{Name}Handler.cs` implementing `IRequestHandler<TQuery,TDto>`, inject repo+`IMapper` via ctor, call repo, `throw new NotFoundException(nameof(Entity), id)` when null, `return _mapper.Map<TDto>(entity)`, (d) optionally add `{Name}Validator.cs : AbstractValidator<TQuery>` with `RuleFor(x => x.Id).GreaterThan(0)`, (e) no manual DI wiring needed (assembly-scanned).
   - Reference composite-key example `GetSubmissionByIdHandler` for ownership-check pattern.

3. **backend-automapper-profile-pattern**
   - description keywords: AutoMapper, Profile, CreateMap, ReverseMap, ForMember, Application/Common/Mapping
   - Workflow: (a) find/create `server/Application/Common/Mapping/{Entity}Profile.cs` in namespace `SmartGrader.Application.Common.Mapping`, (b) class extends `Profile`, mappings in ctor, (c) `CreateMap<Entity, ResponseDto>()` + `.ForMember` for computed/nested fields, (d) `CreateMap<CreateRequestDto, Entity>()` and `CreateMap<UpdateRequestDto, Entity>().ForMember(d=>d.Id/CreatedAt/nav, opt=>opt.Ignore())`, (e) use `.ReverseMap()` only for symmetric value-object DTOs, (f) no registration needed (`AddAutoMapper(assembly)` auto-discovers).
   - Pitfall: keep namespace consistent (`Application.Common.Mapping`), don't replicate the `AssignmentProfile` outlier namespace.

4. **backend-controller-endpoint-pattern**
   - description keywords: Controller, HttpGet/HttpPost/HttpPut/HttpDelete, IMediator, ApiController, Route, endpoint
   - Workflow: (a) pick existing controller under `server/Api/Controllers/` or scaffold new `[ApiController][Route("api/[controller]")]` controller injecting `IMediator`, (b) add action with correct `[Http*]` attribute + route template (`{id:int}`, nested `{parentId:int}/children/{childId:int}`), (c) trailing `CancellationToken cancellationToken` param, `[FromBody]` for POST/PUT bodies, (d) build Query/Command and `await _mediator.Send(...)`, (e) return `Ok`/`CreatedAtAction`/`NoContent` per verb, (f) never try/catch — rely on `GlobalExceptionMiddleware`.
   - **DI convention (explicit pitfall)**: always constructor-inject `IMediator` (`private readonly IMediator _mediator;` set in ctor), matching `LessonsController`/`StudentsController`. Do NOT use `[FromServices]` at the method-level even though valid in .NET 7+ — the codebase has zero precedent for it and it breaks consistency.
   - **CreatedAtAction ID extraction (explicit workflow step)**: for POST actions, the ID passed to `CreatedAtAction`'s route-values must come from the **response DTO returned by the handler** (`created.Id`), never from the request DTO. For nested resources, include the parent route param too, e.g. `CreatedAtAction(nameof(GetAssignmentById), new { lessonId = lessonId, assignmentId = created.Id }, created)` (mirrors `LessonsController.CreateAssignment`). The template asset must show both the flat-resource and nested-resource variants.

**Phase 2 — Cross-check (after Phase 1, quick)** 5. Verify each `SKILL.md` frontmatter `name` matches its folder, description is quoted (contains `:`), and total line count < 500. 6. Optionally add short "See also" links between the 4 skills (repository → handler → mapper → controller form one pipeline) so the AI can chain them when scaffolding a whole new query end-to-end.

## Relevant files (to create)

- `.github/skills/backend-repository-query-pattern/SKILL.md`, `assets/repository-query.template.cs`
- `.github/skills/backend-mediatr-query-handler-pattern/SKILL.md`, `assets/query-handler.template.cs`
- `.github/skills/backend-automapper-profile-pattern/SKILL.md`, `assets/profile.template.cs`
- `.github/skills/backend-controller-endpoint-pattern/SKILL.md`, `assets/controller-endpoint.template.cs`

## Verification

1. Re-read each generated SKILL.md against the create-skill checklist (name matches folder, description quoted + keyword-rich, body has When to Use/Workflow/examples, file <500 lines).
2. Sanity check templates compile conceptually against real examples (`SubmissionRepository`, `GetLessonByIdHandler`, `StudentProfile`, `LessonsController`).
3. Manual test: start a new chat, describe "add a GetByClassNameAsync repo method" and confirm the right skill would be discovered by its description (spot-check keyword overlap).

## Decisions

- Project-scoped (`.github/skills/`), not personal scope — these are repo-specific conventions.
- Each skill gets one small `.cs` template asset for copy-paste, plus a condensed real-code example inline in SKILL.md (not full files, to stay concise).
- Skills are independent/self-contained but will cross-link ("See also") since in practice a new feature touches all 4 layers in sequence (repo → handler → mapper → controller).
- Scope excludes: Command (write) patterns beyond what's needed for context (mentioned only in handler skill as contrast), frontend Angular skills, and fixing the existing commented-out `LessonResultController`/`LessonResultProfile` code (out of scope, noted only as repo memory).
