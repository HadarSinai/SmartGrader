
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly GradeSheetContext _context;

        public LessonRepository(GradeSheetContext context)
        {
            _context = context;
        }


        public async Task<IReadOnlyList<Lesson>> GetAllAsync(int? classId, int? teacherId, CancellationToken ct = default)
        {
            var query = _context.Lessons
                .Include(l => l.Assignments)
                .Include(l => l.Classes)
                .Include(l => l.Course)
                .AsNoTracking()
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(l => l.Classes.Any(c => c.Id == classId.Value));

            if (teacherId.HasValue)
                query = query.Where(l => l.TeacherId == teacherId.Value);

            return await query.ToListAsync(ct);
        }

        public async Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Lessons
                .Include(l => l.Assignments)
                .Include(l => l.Classes)
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id, ct);
        }

        public async Task<IReadOnlyList<Lesson>> GetByDateRangeAsync(DateTime from, DateTime to, int? teacherId, CancellationToken ct = default)
        {
            var query = _context.Lessons
                .Where(l => l.LessonDate >= from && l.LessonDate <= to)
                .Include(l => l.Course)
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(l => l.TeacherId == teacherId.Value);

            return await query
                .OrderBy(l => l.LessonDate)
                .AsNoTracking()
                .ToListAsync(ct);
        }


        public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
        {
            await _context.Lessons.AddAsync(lesson, ct);
        }

        //public Task UpdateAsync(Lesson lesson, CancellationToken ct = default)
        //{
        //    _context.Lessons.Attach(lesson);
        //    return Task.CompletedTask;
        //}

        public Task DeleteAsync(Lesson lesson, CancellationToken ct = default)
        {
            _context.Lessons.Remove(lesson);
            return Task.CompletedTask;
        }
    }
}

