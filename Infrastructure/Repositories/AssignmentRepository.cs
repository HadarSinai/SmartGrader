using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly GradeSheetContext _context;

        public AssignmentRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<Assignment?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<IReadOnlyList<Assignment>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Where(a => a.LessonId == lessonId)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddAsync(Assignment assignment, CancellationToken ct = default)
        {
            await _context.Assignments.AddAsync(assignment, ct);
        }

        public async Task UpdateAsync(Assignment assignment, CancellationToken ct = default)
        {
            _context.Assignments.Update(assignment);
        }

        public async Task DeleteAsync(Assignment assignment, CancellationToken ct = default)
        {
            _context.Assignments.Remove(assignment);
        }
    }
}
