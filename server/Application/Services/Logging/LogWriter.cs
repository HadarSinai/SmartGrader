using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.Logging
{
    /// <summary>
    /// Writes system logs in an isolated DI scope (fresh DbContext) so that a log
    /// write never commits or conflicts with the caller's pending changes — e.g.
    /// when logging from the exception middleware after a failed request.
    /// Best-effort: all failures are swallowed and reported to ILogger only.
    /// </summary>
    public class LogWriter : ILogWriter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<LogWriter> _logger;

        public LogWriter(
            IServiceScopeFactory scopeFactory,
            IEmailSender emailSender,
            ILogger<LogWriter> logger)
        {
            _scopeFactory = scopeFactory;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task WriteAsync(
            string actionType,
            string message,
            string status,
            string systemSource,
            int? userId = null,
            int? lessonId = null,
            int? assignmentId = null,
            CancellationToken ct = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var log = Log.Create(actionType, message, status, systemSource, userId, lessonId, assignmentId);

                await repository.AddAsync(log, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write system log (actionType={ActionType})", actionType);
            }

            if (status == LogStatuses.Error)
            {
                try
                {
                    await _emailSender.SendToAdminAsync(
                        subject: "SmartGrader – שגיאה במערכת",
                        body: $"סוג פעולה: {actionType}\nהודעה: {message}\nמקור: {systemSource}\nזמן (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send error email for log (actionType={ActionType})", actionType);
                }
            }
        }
    }
}
