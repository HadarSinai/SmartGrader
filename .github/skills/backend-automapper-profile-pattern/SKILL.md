---
name: backend-automapper-profile-pattern
description: "Use when adding or reviewing an AutoMapper Profile in the SmartGrader .NET backend: new CreateMap between Entity/DTOs, ForMember computed-field mapping, ReverseMap for symmetric value-object DTOs, or files under server/Application/Common/Mapping. USE FOR: 'add a mapping profile', 'map entity to response DTO', 'AutoMapper CreateMap for X'. NOT for repository queries or MediatR handlers — see the sibling backend-* skills."
---

# Backend AutoMapper Profile Pattern

Mapping profiles live in `server/Application/Common/Mapping/{Entity}Profile.cs`, namespace `SmartGrader.Application.Common.Mapping`. **No manual registration is needed** — `services.AddAutoMapper(assembly)` in `server/Application/DependencyInjection.cs` auto-discovers every `Profile` subclass.

## When to Use

- Adding a new entity and its Response/Create/Update DTOs need mappings.
- Adding a computed or nested field to an existing Response DTO (`.ForMember` with `.MapFrom`).
- Deciding whether a DTO pair needs `.ReverseMap()` (only for symmetric value-object DTOs, not full entities).

## Workflow

1. **Find or create** `server/Application/Common/Mapping/{Entity}Profile.cs` in namespace `SmartGrader.Application.Common.Mapping` (note: `AssignmentProfile.cs` incorrectly uses `SmartGrader.Api.Mapping` — that's a legacy inconsistency, do not copy it for new profiles).
2. **Class extends `Profile`**, all mappings go in the parameterless constructor.
3. **Entity → Response DTO**: `CreateMap<Entity, ResponseDto>()`, add `.ForMember(dest => dest.Computed, opt => opt.MapFrom(src => ...))` for any field not named identically on the entity (e.g. counts derived from a collection).
4. **Create DTO → Entity**: `CreateMap<CreateRequestDto, Entity>()`, ignoring fields that come from elsewhere (route param, server-generated), e.g. `.ForMember(d => d.Id, opt => opt.Ignore())`.
5. **Update DTO → Entity**: `CreateMap<UpdateRequestDto, Entity>()` always ignoring `Id`, `CreatedAt`, and navigation/collection properties the update DTO doesn't carry.
6. **Use `.ReverseMap()` only for symmetric value-object DTOs** (e.g. `TestCaseDto` ↔ `TestCase`), never for full entity↔response mappings.
7. **No DI registration needed** — the profile is picked up automatically as long as it's in the `Application` assembly.

## Real Examples

Computed fields — [`StudentProfile`](../../../server/Application/Common/Mapping/StudentProfile.cs):

```csharp
public class StudentProfile : Profile
{
    public StudentProfile()
    {
        CreateMap<CreateStudentRequestDto, Student>();

        CreateMap<UpdateStudentRequestDto, Student>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Submissions, opt => opt.Ignore())
            .ForMember(dest => dest.LessonResults, opt => opt.Ignore());

        CreateMap<Student, StudentResponseDto>()
            .ForMember(dest => dest.SubmissionsCount,
                       opt => opt.MapFrom(src => src.Submissions != null ? src.Submissions.Count : 0))
            .ForMember(dest => dest.LessonResultsCount,
                       opt => opt.MapFrom(src => src.LessonResults != null ? src.LessonResults.Count : 0));
    }
}
```

Value-object `.ReverseMap()` — [`AssignmentProfile`](../../../server/Application/Common/Mapping/AssignmentProfile.cs) (note the outlier `SmartGrader.Api.Mapping` namespace — don't repeat it):

```csharp
// TestCase <-> TestCaseDto is symmetric, so ReverseMap is appropriate here
CreateMap<TestCaseDto, TestCase>().ReverseMap();

CreateMap<Assignment, AssignmentResponseDto>();

CreateMap<CreateAssignmentRequestDto, Assignment>()
    .ForMember(d => d.Id, opt => opt.Ignore())
    .ForMember(d => d.LessonId, opt => opt.Ignore())   // comes from the route/Command, not the body
    .ForMember(d => d.CreatedAt, opt => opt.Ignore())
    .ForMember(d => d.TestsJson, opt => opt.Ignore());
```

## Template

Copy-paste starting point: [assets/profile.template.cs](./assets/profile.template.cs)

## Pitfalls

- Keep the namespace `SmartGrader.Application.Common.Mapping` — don't replicate the `AssignmentProfile` outlier (`SmartGrader.Api.Mapping`) in new profiles.
- Don't add `.ReverseMap()` between a full Entity and its Response DTO — only use it for small symmetric value-object DTOs.
- Always `.Ignore()` `Id`, `CreatedAt`, and navigation/collection properties on Update-DTO → Entity maps; forgetting this can null out relations on save.
- No manual registration — don't add anything to `DependencyInjection.cs` for a new profile; `AddAutoMapper(assembly)` already scans it.

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — handlers call `_mapper.Map<TDto>(entity)` using these profiles.
