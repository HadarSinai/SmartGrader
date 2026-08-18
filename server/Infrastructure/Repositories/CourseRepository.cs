using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly GradeSheetContext _context;

        public CourseRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Course>> GetAllAsync(int? teacherId, CancellationToken ct = default)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(c => c.TeacherId == teacherId.Value);

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<Course?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            // ⚠️ בכוונה בלי AsNoTracking — נטען לצורך read-then-mutate ב-Update/Delete handlers
            return await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<Course?> GetByNameAndTeacherAsync(string name, int teacherId, CancellationToken ct = default)
        {
            return await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name && c.TeacherId == teacherId, ct);
        }

        public async Task AddAsync(Course course, CancellationToken ct = default)
        {
            await _context.Courses.AddAsync(course, ct);
        }

        public Task DeleteAsync(Course course, CancellationToken ct = default)
        {
            _context.Courses.Remove(course);
            return Task.CompletedTask;
        }
    }
}
