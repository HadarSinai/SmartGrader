using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class SchoolClassRepository : ISchoolClassRepository
    {
        private readonly GradeSheetContext _context;
        public SchoolClassRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<SchoolClass>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            var query = _context.SchoolClasses
                .AsNoTracking()
                .Include(c => c.Students)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(c => !c.IsArchived);

            return await query
                .OrderByDescending(c => c.AcademicYear)
                .ThenBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<SchoolClass?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.SchoolClasses
                .AsNoTracking()
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<SchoolClass?> GetByNameAndYearAsync(string name, int academicYear, CancellationToken ct = default)
        {
            return await _context.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name && c.AcademicYear == academicYear, ct);
        }

        public async Task<IReadOnlyList<SchoolClass>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        {
            return await _context.SchoolClasses
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct);
        }

        public async Task AddAsync(SchoolClass schoolClass, CancellationToken ct = default)
        {
            await _context.SchoolClasses.AddAsync(schoolClass, ct);
        }

        public Task UpdateAsync(SchoolClass schoolClass, CancellationToken ct = default)
        {
            _context.SchoolClasses.Update(schoolClass);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(SchoolClass schoolClass, CancellationToken ct = default)
        {
            _context.SchoolClasses.Remove(schoolClass);
            return Task.CompletedTask;
        }

        public async Task<int> ArchiveAllActiveAsync(CancellationToken ct = default)
        {
            return await _context.SchoolClasses
                .Where(c => !c.IsArchived)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsArchived, true), ct);
        }
    }
}
