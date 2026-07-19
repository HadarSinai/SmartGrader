using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.Infrastructure.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly GradeSheetContext _db;

        public LogRepository(GradeSheetContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<Log>> GetLatestAsync(int maxCount, CancellationToken ct = default)
        {
            return await _db.Logs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Take(maxCount)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Log log, CancellationToken ct = default)
        {
            await _db.Logs.AddAsync(log, ct);
        }

        public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default)
        {
            return await _db.Logs
                .Where(l => l.Timestamp < cutoffUtc)
                .ExecuteDeleteAsync(ct);
        }
    }
}
