using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface ILogRepository
    {
        /// <summary>Latest logs first (Timestamp desc), capped by <paramref name="maxCount"/>.</summary>
        Task<IReadOnlyList<Log>> GetLatestAsync(int maxCount, CancellationToken ct = default);
        Task AddAsync(Log log, CancellationToken ct = default);
        /// <summary>Deletes all logs older than the cutoff. Returns the number of deleted rows.</summary>
        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
    }
}
