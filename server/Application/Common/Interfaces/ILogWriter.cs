using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Interfaces
{
    /// <summary>
    /// Best-effort system event logger: writes a row to the Logs table and,
    /// for Status=Error logs, emails the admin. Never throws — a failed log
    /// write must not fail the main operation.
    /// </summary>
    public interface ILogWriter
    {
        Task WriteAsync(
            string actionType,
            string message,
            string status,
            string systemSource,
            int? userId = null,
            int? lessonId = null,
            int? assignmentId = null,
            CancellationToken ct = default);
    }
}
