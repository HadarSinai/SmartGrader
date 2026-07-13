// 1) Add to server/Domain/Abstractions/I{Entity}Repository.cs
//
// Task<IReadOnlyList<{Entity}>> GetBy{Criteria}Async(int {criteria}, CancellationToken ct = default);
// Task<{Entity}?> GetBy{Criteria}Async(int {criteria}, CancellationToken ct = default); // single-result variant

// 2) Implement in server/Infrastructure/Repositories/{Entity}Repository.cs

using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class {Entity}Repository : I{Entity}Repository
    {
        private readonly GradeSheetContext _context;

        public {Entity}Repository(GradeSheetContext context)
        {
            _context = context;
        }

        // Multiple-results query
        public async Task<IReadOnlyList<{Entity}>> GetBy{Criteria}Async(int {criteria}, CancellationToken ct = default)
        {
            return await _context.{Entities}
                .Where(e => e.{Criteria} == {criteria})
                .Include(e => e.{NavigationProperty})
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // Single-result query
        public async Task<{Entity}?> GetBy{Criteria}Async(int {criteria}, CancellationToken ct = default)
        {
            return await _context.{Entities}
                .Include(e => e.{NavigationProperty})
                .FirstOrDefaultAsync(e => e.{Criteria} == {criteria}, ct);
        }
    }
}

// 3) Only if I{Entity}Repository/{Entity}Repository are brand new, register in
//    server/Infrastructure/DependencyInjection.cs:
//
// services.AddScoped<I{Entity}Repository, {Entity}Repository>();
