---
name: backend-repository-query-pattern
description: "Use when adding or reviewing a Repository query method in the SmartGrader .NET backend: new GetBy...Async lookups, EF Core Where/Include/AsNoTracking queries, I{Entity}Repository interfaces in Domain/Abstractions, or repository implementations in Infrastructure/Repositories. USE FOR: 'add a repository method', 'query the database for X', 'GetByStudentIdAsync-style method'. NOT for MediatR handlers, AutoMapper profiles, or controller endpoints (see the sibling backend-* skills for those)."
---

# Backend Repository Query Pattern

SmartGrader repositories are thin EF Core wrappers: one interface per entity in `server/Domain/Abstractions/I{Entity}Repository.cs`, one implementation in `server/Infrastructure/Repositories/{Entity}Repository.cs`. There is **no generic base repository** — every method is written explicitly.

## When to Use

- Adding a new lookup like "get all submissions for a student" or "get lesson by class name".
- Reviewing/fixing an existing repository method for consistency with the codebase.
- Deciding whether a query belongs in the repository vs. inline in a handler (it always belongs in the repository).

## Workflow

1. **Add the signature** to the interface in `server/Domain/Abstractions/I{Entity}Repository.cs`. Always take a trailing `CancellationToken ct = default`.
2. **Implement it** in `server/Infrastructure/Repositories/{Entity}Repository.cs`, injecting `GradeSheetContext _context` via the constructor (already present on existing repos).
3. **Pick the return shape**:
   - Multiple rows → `Task<IReadOnlyList<T>>` + `.Where(...).ToListAsync(ct)`.
   - Single row → `Task<T?>` + `.FirstOrDefaultAsync(predicate, ct)`.
4. **Add `.Include(...)` for navigation properties** the caller will need (mirrors existing `.Include(s => s.Student).Include(s => s.Assignment)`), and always end read queries with `.AsNoTracking()`.
5. **Never call `SaveChangesAsync`** inside a repository method — persistence goes through `IUnitOfWork`, used only by command handlers, not query methods.
6. **Register in DI only if new**: if you created a brand-new `I{Entity}Repository`/`{Entity}Repository` pair (not just a new method on an existing one), add `services.AddScoped<I{Entity}Repository, {Entity}Repository>();` in `server/Infrastructure/DependencyInjection.cs`.

## Real Example

[`SubmissionRepository.GetByStudentIdAsync`](../../../server/Infrastructure/Repositories/SubmissionRepository.cs):

```csharp
public async Task<IReadOnlyList<Submission>> GetByStudentIdAsync(int studentId, CancellationToken ct = default)
{
    return await _context.Submissions
        .Where(s => s.StudentId == studentId)
        .Include(s => s.Student)
        .Include(s => s.Assignment)
        .AsNoTracking()
        .ToListAsync(ct);
}
```

Single-entity variant, same file:

```csharp
public async Task<Submission?> GetByIdAsync(int id, CancellationToken ct = default)
{
    return await _context.Submissions
        .Include(s => s.Student)
        .Include(s => s.Assignment)
        .FirstOrDefaultAsync(s => s.Id == id, ct);
}
```

## Template

Copy-paste starting point: [assets/repository-query.template.cs](./assets/repository-query.template.cs)

## Pitfalls

- Don't call `SaveChangesAsync` from a repository — queries are read-only; writes go through `IUnitOfWork` in command handlers.
- Don't forget `AsNoTracking()` on read queries — the existing codebase does this consistently for performance.
- Don't skip `CancellationToken ct = default` even on simple methods — every existing method takes it.
- Don't build a generic/base repository — this codebase intentionally keeps one explicit interface+class per entity.

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the handler that calls this repository method.
