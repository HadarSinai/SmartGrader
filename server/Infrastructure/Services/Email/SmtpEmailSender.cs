using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartGrader.Application.Common.Interfaces;

namespace SmartGrader.Infrastructure.Services.Email
{
    /// <summary>
    /// Sends emails via SMTP — either to the admin address (AdminUser:Email) or to an
    /// explicit recipient. When SMTP is not configured it no-ops with a warning; the
    /// application must keep working without email.
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
            if (string.IsNullOrWhiteSpace(_adminEmail))
            {
                _logger.LogWarning(
                    "Email not sent — admin email is not configured (subject: {Subject})",
                    subject);
                return;
            }

            await SendAsync(_adminEmail, subject, body, ct);
        }

        /// <summary>
        /// ⚠️ מדווחת על SMTP שאינו מוגדר במקום להשתיק — ר' ההסבר ב-<see cref="IEmailSender"/>.
        /// חריגה אמיתית בשליחה עדיין נזרקת: הקורא כאן הוא handler שיודע לתפוס אותה,
        /// בשונה מהתראות השגיאה שרצות מתוך טיפול-בשגיאה ואסור להן להיכשל.
        /// </summary>
        public async Task<bool> SendAsync(
            string to,
            string subject,
            string body,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                _logger.LogWarning(
                    "Email not sent — SMTP host is not configured (subject: {Subject})",
                    subject);
                return false;
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
            message.To.Add(to);

            await client.SendMailAsync(message, ct);
            return true;
        }
    }
}
