using System.Text;
using MediatR;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Notifications;
using SmartGrader.Application.Services.Notifications;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Notifications.SendTeacherDigest
{
    public class SendTeacherDigestHandler : IRequestHandler<SendTeacherDigestCommand, int>
    {
        private readonly ISubmissionRepository _submissions;
        private readonly IUserRepository _users;
        private readonly ClassSignalDetector _detector;
        private readonly IEmailSender _emailSender;
        private readonly IClientUrlProvider _clientUrl;
        private readonly ILogWriter _logWriter;

        public SendTeacherDigestHandler(
            ISubmissionRepository submissions,
            IUserRepository users,
            ClassSignalDetector detector,
            IEmailSender emailSender,
            IClientUrlProvider clientUrl,
            ILogWriter logWriter)
        {
            _submissions = submissions;
            _users = users;
            _detector = detector;
            _emailSender = emailSender;
            _clientUrl = clientUrl;
            _logWriter = logWriter;
        }

        public async Task<int> Handle(SendTeacherDigestCommand request, CancellationToken cancellationToken)
        {
            // ⚠️ קריאת מסד אחת לכל הריצה, ולא אחת לכל מורה. הפילוח לפי מורה נעשה כאן על
            // TeacherId של השיעור — אותו כלל בעלות בדיוק כמו בשאילתה של הפעמון, ולכן
            // הגשה של מורה אחת לעולם אינה יכולה להיכנס לדיג'סט של אחרת.
            var all = await _submissions.GetConcludedInRangeAsync(
                request.FromUtc, request.ToUtc, teacherId: null, ct: cancellationToken);

            if (all.Count == 0)
                return 0;

            var teachers = await _users.GetByRoleAsync(UserRole.Teacher, cancellationToken);
            var byId = teachers.ToDictionary(t => t.Id);

            var sent = 0;

            foreach (var group in all
                .Where(s => s.Assignment is not null && s.Assignment.Lesson is not null)
                .GroupBy(s => s.Assignment.Lesson.TeacherId))
            {
                // מורה שנמחקה, או שורה שאינה מורה — אין למי לשלוח, וזו אינה תקלה.
                if (!byId.TryGetValue(group.Key, out var teacher))
                    continue;

                // ⚠️ מורה בלי מייל מדולגת בשקט. Email הוא nullable בסכימה (שורות שנוצרו לפני
                // המיגרציה), והחובה נאכפת רק ב-validator — כלומר המקרה הזה קיים בפועל.
                if (string.IsNullOrWhiteSpace(teacher.Email))
                    continue;

                var signals = _detector.Detect(group.ToList());

                // 🔴 יום בלי מה לדווח עליו לא שולח כלום. לא "אין חדשות היום", אף פעם — זה
                // ההבדל בין דיג'סט שקוראים לבין דיג'סט שמסננים לתיקייה.
                if (signals.Count == 0)
                    continue;

                if (await TrySendAsync(teacher, signals, request.FromUtc, cancellationToken))
                    sent++;
            }

            return sent;
        }

        /// <summary>
        /// ⚠️ כל שליחה עטופה בנפרד. בלי זה, מורה אחת עם כתובת פסולה מפילה את החריגה החוצה
        /// ומבטלת את שאר הריצה — כל השאר לא היו מקבלות דבר, בלי שאיש ידע.
        /// </summary>
        private async Task<bool> TrySendAsync(
            User teacher, IReadOnlyList<ClassSignalDto> signals, DateTime dayUtc, CancellationToken ct)
        {
            try
            {
                var sent = await _emailSender.SendAsync(
                    to: teacher.Email!,
                    subject: $"SmartGrader – סיכום יומי ({signals.Count} התראות)",
                    body: BuildBody(teacher.FullName, signals, dayUtc),
                    ct: ct);

                if (!sent)
                {
                    await LogFailureAsync(
                        "SMTP אינו מוגדר — הדיג'סט היומי לא נשלח", teacher.Id, ct);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await LogFailureAsync(
                    $"שליחת הדיג'סט היומי נכשלה: {ex.Message}", teacher.Id, ct);
                return false;
            }
        }

        /// <summary>
        /// רושמת את התקלה כ-<c>Status=Error</c>, ולכן היא מגיעה גם למסך הלוגים של המנהלת
        /// וגם להתראה במייל אליה. בלי זה, דיג'סט שבור נראה בדיוק כמו יום שקט.
        /// </summary>
        private Task LogFailureAsync(string message, int userId, CancellationToken ct) =>
            _logWriter.WriteAsync(
                actionType: LogActionTypes.TeacherDigestEmailFailed,
                message: message,
                status: LogStatuses.Error,
                systemSource: LogSystemSources.Api,
                userId: userId,
                ct: ct);

        private string BuildBody(string fullName, IReadOnlyList<ClassSignalDto> signals, DateTime dayUtc)
        {
            var body = new StringBuilder();
            body.AppendLine($"שלום {fullName},");
            body.AppendLine();
            body.AppendLine($"להלן סיכום הפעילות מיום {dayUtc.ToLocalTime():dd.MM.yyyy}:");
            body.AppendLine();

            foreach (var lesson in signals.GroupBy(s => new { s.LessonId, s.LessonSubject }))
            {
                body.AppendLine($"■ {Display(lesson.Key.LessonSubject, lesson.Key.LessonId)}");

                foreach (var signal in lesson)
                {
                    body.AppendLine($"  • {signal.Message}");

                    var link = LinkTo(signal.LessonId);
                    if (link is not null)
                        body.AppendLine($"    {link}");
                }

                body.AppendLine();
            }

            body.AppendLine("בהצלחה,");
            body.AppendLine("מערכת SmartGrader");
            return body.ToString();
        }

        private static string Display(string subject, int lessonId) =>
            string.IsNullOrWhiteSpace(subject) ? $"שיעור {lessonId}" : subject;

        /// <summary>
        /// ⚠️ מחזירה <c>null</c> כשה-App:ClientBaseUrl אינו מוגדר. שורת קישור שבורה במייל
        /// גרועה משורה חסרה — והתוכן עצמו עומד בפני עצמו גם בלעדיה.
        /// </summary>
        private string? LinkTo(int lessonId) =>
            _clientUrl.IsConfigured ? $"{_clientUrl.BaseUrl}/lessons/{lessonId}/assignments" : null;
    }
}
