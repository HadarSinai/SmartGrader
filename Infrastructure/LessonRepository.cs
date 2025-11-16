// Infrastructure/Repositories/LessonRepository.cs
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;

namespace SmartGrader.Infrastructure.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly GradeSheetContext _context;

        public LessonRepository(GradeSheetContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Lesson>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Lessons
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Lessons
                .Include(l => l.Assignments) // אם תרצי גם משימות
                .FirstOrDefaultAsync(l => l.Id == id, ct);
        }

        public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
        {
            await _context.Lessons.AddAsync(lesson, ct);
        }

        public Task UpdateAsync(Lesson lesson, CancellationToken ct = default)
        {
            _context.Lessons.Update(lesson);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Lesson lesson, CancellationToken ct = default)
        {
            _context.Lessons.Remove(lesson);
            return Task.CompletedTask;
        }
    }
}
