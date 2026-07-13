# Plan: Lesson Result Progress (GET only, computed on the fly)

## TL;DR

Add a GET endpoint `GET api/lesson-results/{studentId}/{lessonId}` that returns a student's lesson result
PLUS two computed (not stored) fields: `TotalAssignments` (how many assignments exist in the lesson) and
`CompletedAssignments` (how many of them the student has a `Done` submission for). No DB migration, no new
entity columns — everything is calculated live from `Assignments` + `Submissions` tables. No POST endpoint
(explicitly excluded per user request).

## Steps

### Phase 1 — Repository support (parallel with Phase 2)

1. Add `Task<IReadOnlyList<Submission>> GetByStudentAndLessonAsync(int studentId, int lessonId, CancellationToken ct = default)`
   to `server/Domain/Abstractions/ISubmissionRepository.cs`.
2. Implement it in `server/Infrastructure/Repositories/SubmissionRepository.cs`, following the existing
   `GetByStudentIdAsync` pattern (uses `_context.Submissions.Where(...).Include(s => s.Student).Include(s => s.Assignment).AsNoTracking()`).
   Filter: `s.StudentId == studentId && s.Assignment.LessonId == lessonId`.

### Phase 2 — DTO

3. Extend `server/Application/Dtos/LessonResults/LessonResultResponseDto.cs` with:
   - `public int TotalAssignments { get; set; }`
   - `public int CompletedAssignments { get; set; }`

### Phase 3 — CQRS Query (depends on Phase 1 & 2)

4. Create `server/Application/UseCases/LessonResults/GetLessonResult/GetLessonResultQuery.cs`:
   `public record GetLessonResultQuery(int StudentId, int LessonId) : IRequest<LessonResultResponseDto?>;`
   (Note: this folder currently exists but is empty — previous attempt was undone by user.)
5. Create `server/Application/UseCases/LessonResults/GetLessonResult/GetLessonResultHandler.cs`:
   - Inject `ILessonResultRepository`, `IAssignmentRepository`, `ISubmissionRepository`, `IMapper`.
   - `total = (await _assignmentRepo.GetByLessonIdAsync(request.LessonId, ct)).Count`
   - `doneSubmissions = (await _submissionRepo.GetByStudentAndLessonAsync(request.StudentId, request.LessonId, ct)).Where(s => s.Status == SubmissionStatus.Done)`
   - `completed = doneSubmissions.Select(s => s.AssignmentId).Distinct().Count()`
   - `existingResult = await _lessonResultRepo.GetAsync(request.StudentId, request.LessonId, ct)`
   - Build response DTO: if `existingResult` is not null, `_mapper.Map<LessonResultResponseDto>(existingResult)`, else construct a default DTO (Id=0, IsComplete=false, FinalScore=null) with `StudentId`/`LessonId` set.
   - Set `response.TotalAssignments = total; response.CompletedAssignments = completed;` and return it (never return null — always return a DTO so client can show "0/5" even before any `LessonResult` row exists).

### Phase 4 — Enable AutoMapper profile (depends on Phase 2)

6. Uncomment `server/Application/Common/Mapping/LessonResultProfile.cs`. Keep `CreateMap<LessonResult, LessonResultResponseDto>().ReverseMap()`
   but the reverse map isn't needed for GET — verify `ReverseMap()` doesn't break anything with the new
   `TotalAssignments`/`CompletedAssignments` fields (they don't exist on the entity, so AutoMapper will just
   leave them at default on forward map — handler overwrites them after mapping anyway, so order matters:
   map first, then set the two computed fields).
   Remove the commented-out `CompleteLessonRequestDto -> CompleteLessonCommand` map line too, or leave commented
   since POST/Complete is out of scope — **decision needed**: keep as-is (commented) or fully remove. Recommend
   leaving that specific line commented since Complete flow stays disabled, only uncomment the class wrapper +
   the `LessonResult -> LessonResultResponseDto` map.

### Phase 5 — Controller endpoint (depends on Phase 3)

7. In `server/Api/Controllers/LessonResultController.cs`, add:
   ```
   [HttpGet("{studentId:int}/{lessonId:int}")]
   public async Task<IActionResult> Get(int studentId, int lessonId, CancellationToken ct)
   {
       var result = await _mediator.Send(new GetLessonResultQuery(studentId, lessonId), ct);
       return Ok(result);
   }
   ```
   Keep the `Complete` POST method commented out (explicitly out of scope).

### Phase 6 — Verify DI registration

8. Confirm `Application/DependencyInjection.cs` registers MediatR handlers via assembly scan (should auto-pick-up
   new handler — verify no explicit handler list needs updating).
9. Confirm `Infrastructure/DependencyInjection.cs` already registers `ISubmissionRepository`, `IAssignmentRepository`,
   `ILessonResultRepository` as scoped (already true per prior exploration).

## Relevant files

- `server/Domain/Abstractions/ISubmissionRepository.cs` — add `GetByStudentAndLessonAsync`
- `server/Infrastructure/Repositories/SubmissionRepository.cs` — implement new method (pattern: existing `GetByStudentIdAsync`)
- `server/Domain/Abstractions/IAssignmentRepository.cs` — reuse existing `GetByLessonIdAsync` (no change needed)
- `server/Application/Dtos/LessonResults/LessonResultResponseDto.cs` — add 2 fields
- `server/Application/UseCases/LessonResults/GetLessonResult/GetLessonResultQuery.cs` — new (folder exists, empty)
- `server/Application/UseCases/LessonResults/GetLessonResult/GetLessonResultHandler.cs` — new
- `server/Application/Common/Mapping/LessonResultProfile.cs` — uncomment `LessonResult -> LessonResultResponseDto` map
- `server/Api/Controllers/LessonResultController.cs` — add GET endpoint only, POST stays commented
- `server/Domain/Entities/Submission.cs` — reference only (`Status`, `AssignmentId`, `StudentId`)
- `server/Domain/Entities/LessonResult.cs` — reference only, no changes

## Verification

1. Build the solution (`dotnet build server/SmartGrader.sln`) — confirm no compile errors after uncommenting mapping profile.
2. Manual test via `server/Api/WebApplication2.http` or Swagger: `GET /api/lesson-results/{studentId}/{lessonId}` for:
   - A student/lesson with no `LessonResult` row yet but with some `Done` submissions → expect `Id=0`, `IsComplete=false`, correct `TotalAssignments`/`CompletedAssignments`.
   - A student/lesson with an existing completed `LessonResult` → expect real `FinalScore`/`IsComplete` plus correct counts.
   - A lesson with 0 assignments → expect `TotalAssignments=0`, `CompletedAssignments=0`, no divide-by-zero anywhere (no division happens server-side, ratio is a client concern).
3. Confirm no migration was generated (no new files under `server/Infrastructure/Migrations/`).

## Decisions

- **No DB schema changes** — counts are computed dynamically each request (per user's explicit preference over
  storing/denormalizing counts on `LessonResult`).
- **GET only** — no POST/Complete endpoint work in this plan (stays commented out, per earlier explicit user request).
- **AiWorker is NOT touched** — since counts are computed on read, not maintained incrementally on write.
- Handler always returns a DTO (never 404/null) even if no `LessonResult` row exists yet, so the client can
  render progress ("0/5") before the lesson is marked complete.

## Further Considerations

1. Route shape: `GET api/lesson-results/{studentId}/{lessonId}` (two path params) vs. query string
   `GET api/lesson-results?studentId=&lessonId=`. Recommend path params for consistency with `LessonsController`'s
   `{id:int}` pattern. Open question for user confirmation if desired.
2. Frontend work (Angular model/service/component to display "3/5 completed") is explicitly out of scope for
   this plan and would be a follow-up.
