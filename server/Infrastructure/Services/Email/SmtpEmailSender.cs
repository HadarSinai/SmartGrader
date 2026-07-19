using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartGrader.Application.Common.Interfaces;

namespace SmartGrader.Infrastructure.Services.Email
{
    /// <summary>
    /// Sends emails to the admin address (AdminUser:Email) via SMTP.
    /// When SMTP or the admin email is not configured, it no-ops with a warning —
    /// the application must keep working without email.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly string _adminEmail;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<SmtpOptions> options,
            IConfiguration configuration,
            ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _adminEmail = configuration["AdminUser:Email"] ?? "";
            _logger = logger;
        }

        public async Task SendToAdminAsync(string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_adminEmail))
            {
                _logger.LogWarning(
                    "Email not sent — SMTP host or admin email is not configured (subject: {Subject})",
                    subject);
                return;
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.User, _options.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(
                    string.IsNullOrWhiteSpace(_options.From) ? _options.User : _options.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(_adminEmail);

            await client.SendMailAsync(message, ct);
        }
    }
}
