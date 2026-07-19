using MediatR;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.UseCases.Logs.DeleteOldLogs;

namespace SmartGrader.Api.BackgroundServices;

/// <summary>
/// Daily Hangfire job: deletes logs older than Logs:RetentionDays (default 30)
/// so the Logs table never grows unbounded.
/// </summary>
public class LogCleanupJob : ILogCleanupJob
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogCleanupJob> _logger;

    public LogCleanupJob(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<LogCleanupJob> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var retentionDays = _configuration.GetValue<int?>("Logs:RetentionDays") ?? 30;
        var deleted = await _mediator.Send(new DeleteOldLogsCommand(retentionDays));

        if (deleted > 0)
            _logger.LogInformation(
                "Log cleanup: deleted {Deleted} logs older than {Days} days", deleted, retentionDays);
    }
}
