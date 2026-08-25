using MediatR;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.Services.Notifications;
using SmartGrader.Application.UseCases.Notifications.SendTeacherDigest;

namespace SmartGrader.Api.BackgroundServices;

/// <summary>
/// עבודת Hangfire יומית: שולחת לכל מורה סיכום אחד של הסיגנלים מאתמול.
/// <para>
/// החלון מחושב כאן ולא ב-handler, מאותו נימוק כמו ב-<see cref="LogCleanupJob"/>: העבודה
/// יודעת "עכשיו", ה-handler יודע רק מה עושים עם טווח.
/// </para>
/// </summary>
public class TeacherDigestJob : ITeacherDigestJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<TeacherDigestJob> _logger;

    public TeacherDigestJob(IMediator mediator, ILogger<TeacherDigestJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var (fromUtc, toUtc) = ClassSignalPeriod.PreviousDay(DateTime.UtcNow);
        var sent = await _mediator.Send(new SendTeacherDigestCommand(fromUtc, toUtc));

        // יום שקט אינו כותב שורה — הוא המצב הרגיל, ורישום שלו היה מטביע את הימים שכן קרה בהם משהו.
        if (sent > 0)
            _logger.LogInformation(
                "Teacher digest: sent {Sent} emails for {Day:yyyy-MM-dd}", sent, fromUtc);
    }
}
