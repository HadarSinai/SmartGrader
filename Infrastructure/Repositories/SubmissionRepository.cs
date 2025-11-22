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

        public async Task<IReadOnlyList<Submission>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Submissions
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
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task AddAsync(Submission submission, CancellationToken ct = default)
        {
            await _context.Submissions.AddAsync(submission, ct);
        }

        public Task UpdateAsync(Submission submission, CancellationToken ct = default)
        {
            _context.Submissions.Update(submission);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Submission submission, CancellationToken ct = default)
        {
            _context.Submissions.Remove(submission);
            return Task.CompletedTask;
        }
    }
}
