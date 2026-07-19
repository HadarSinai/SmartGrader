

using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly GradeSheetContext _context;
        public StudentRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default)
            => GetAllAsync(includeArchived: true, ct);

        public async Task<IReadOnlyList<Student>> GetAllAsync(bool includeArchived, CancellationToken ct = default)
        {
            var query = _context.Students
                .AsNoTracking()
                .Include(s => s.Class)
                .Include(s => s.Submissions)
                .Include(s => s.LessonResults)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(s => !s.Class.IsArchived);

            return await query.ToListAsync(ct);
        }
        public async Task<Student?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Students
                .AsNoTracking()
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public async Task<Student?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        }
        public async Task AddAsync(Student student, CancellationToken ct = default)
        {
            await _context.Students.AddAsync(student, ct);
        }
        public Task UpdateAsync(Student student, CancellationToken ct = default)
        {
            _context.Students.Update(student);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Student student, CancellationToken ct = default)
        {
            _context.Students.Remove(student);
            return Task.CompletedTask;
        }
    }
}


