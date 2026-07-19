namespace SmartGrader.Application.Common.Interfaces
{
    /// <summary>
    /// Sends notification emails to the system admin.
    /// Implementations must be best-effort: never throw when SMTP is not configured.
    /// </summary>
    public interface IEmailSender
    {
        Task SendToAdminAsync(string subject, string body, CancellationToken ct = default);
    }
}
