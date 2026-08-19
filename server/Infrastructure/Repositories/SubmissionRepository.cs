
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
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly GradeSheetContext _context;

        public SubmissionRepository(GradeSheetContext context)
        {
            _context = context;
        }
        //רק למורות
        public async Task<IReadOnlyList<Submission>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        // teacherId מסנן לפי בעלות על השיעור שמתחת לתרגיל — אותו ניב בדיוק כמו ב-GetRecentGradedAsync,
        // שהיה עד כה היחיד שהשתמש בו. null = מנהל/ת, תלמידה על נתוני עצמה, או קורא מערכת.
        public async Task<IReadOnlyList<Submission>> GetByStudentIdAsync(int studentId, int? teacherId, CancellationToken ct = default)
        {
            var query = _context.Submissions
                .Where(s => s.StudentId == studentId)
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(s => s.Assignment.Lesson.TeacherId == teacherId.Value);

            return await query
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Submission>> GetByStudentAndLessonAsync(int studentId, int lessonId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId && s.Assignment.LessonId == lessonId)
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Submission?> GetByStudentAndAssignmentAsync(int studentId, int assignmentId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId && s.AssignmentId == assignmentId)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        // כל ההגשות של שיעור בשאילתה אחת — מחליף את ה-N+1 שב-ExportLessonResultsHandler
        // (קריאת GetByStudentAndLessonAsync אחת לכל תלמידה, כל אחת עם שני Include).
        public async Task<IReadOnlyList<Submission>> GetByLessonIdAsync(int lessonId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Where(s => s.Assignment.LessonId == lessonId)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<int> CountByLessonIdAsync(int lessonId, CancellationToken ct = default)
            => await _context.Submissions.CountAsync(s => s.Assignment.LessonId == lessonId, ct);

        public async Task<int> CountByAssignmentIdAsync(int assignmentId, CancellationToken ct = default)
            => await _context.Submissions.CountAsync(s => s.AssignmentId == assignmentId, ct);

        public async Task<int> CountByStudentIdAsync(int studentId, CancellationToken ct = default)
            => await _context.Submissions.CountAsync(s => s.StudentId == studentId, ct);

        public async Task<IReadOnlyList<Submission>> GetRecentGradedAsync(int limit, int? teacherId, int? studentId, CancellationToken ct = default)
        {
            var query = _context.Submissions
                .Where(s => s.Status == SubmissionStatus.Done)
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Lesson)
                .AsQueryable();

            // ⚠️ הסינון חייב לקרות לפני ה-Take — אחרת לוקחים את 20 הגלובליים ואז מסננים ל-3
            if (teacherId.HasValue)
                query = query.Where(s => s.Assignment.Lesson.TeacherId == teacherId.Value);

            if (studentId.HasValue)
                query = query.Where(s => s.StudentId == studentId.Value);

            return await query
                .OrderByDescending(s => s.GradedAt ?? s.SubmittedAt)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // ⚠️ בלי AsNoTracking בכוונה — הקוראים (Update/Delete/AiWorker) משנים את הישות ושומרים דרך UnitOfWork.
        public async Task<Submission?> GetByIdAsync(int id, int? teacherId, CancellationToken ct = default)
        {
            var query = _context.Submissions
                .Where(s => s.Id == id)
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(s => s.Assignment.Lesson.TeacherId == teacherId.Value);

            return await query
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddAsync(Submission submission, CancellationToken ct = default)
        {
            await _context.Submissions.AddAsync(submission, ct);
        }

        //public Task UpdateAsync(Submission submission, CancellationToken ct = default)
        //{
        //    _context.Submissions.Attach(submission);
        //    return Task.CompletedTask;
        //}

        public Task DeleteAsync(Submission submission, CancellationToken ct = default)
        {
            _context.Submissions.Remove(submission);
            return Task.CompletedTask;
        }
    }
}


