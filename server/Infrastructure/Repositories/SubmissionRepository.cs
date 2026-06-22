using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
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
        public async Task<IReadOnlyList<Submission>> GetByStudentIdAsync(int studentId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Submission?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
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
