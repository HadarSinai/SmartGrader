namespace SmartGrader.Domain.Entities
{
    /// <summary>Well-known values for <see cref="Log.ActionType"/>.</summary>
    public static class LogActionTypes
    {
        public const string AiGradingStarted = "AiGradingStarted";
        public const string AiGradingCompleted = "AiGradingCompleted";
        public const string CompilationFailed = "CompilationFailed";
        public const string AiFailed = "AiFailed";
        public const string JudgeUnavailable = "JudgeUnavailable";
        public const string UnhandledError = "UnhandledError";

        /// <summary>
        /// תקלה תפעולית במסלול שחזור הסיסמה. ⚠️ נרשם רק על <b>כשל</b> ולא על כל בקשה:
        /// רישום של כל בקשה היה יוצר במסך הלוגים רשימה של כתובות מייל שקיימות במערכת.
        /// </summary>
        public const string PasswordResetEmailFailed = "PasswordResetEmailFailed";

        /// <summary>
        /// הדיג'סט היומי למורה לא נשלח. ⚠️ נרשם רק על <b>כשל</b>, ובכוונה: יום בלי סיגנלים
        /// אינו שולח מייל ואינו כותב שורה. בלי הרישום הזה, SMTP שבור נראה בדיוק כמו יום שקט —
        /// <c>SmtpEmailSender</c> מחזיר <c>false</c> בשקט כשאינו מוגדר.
        /// </summary>
        public const string TeacherDigestEmailFailed = "TeacherDigestEmailFailed";
    }

    /// <summary>Well-known values for <see cref="Log.Status"/>.</summary>
    public static class LogStatuses
    {
        public const string Success = "Success";
        public const string Error = "Error";
    }

    /// <summary>Well-known values for <see cref="Log.SystemSource"/>.</summary>
    public static class LogSystemSources
    {
        public const string AiWorker = "AiWorker";
        public const string Api = "Api";
    }

    public class Log
    {
        public int Id { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public int? UserId { get; private set; }
        public int? LessonId { get; private set; }
        public int? AssignmentId { get; private set; }
        public string ActionType { get; private set; } = "";
        public string Message { get; private set; } = "";
        public string Status { get; private set; } = "";
        public string SystemSource { get; private set; } = "";

        private Log() { }

        public static Log Create(
            string actionType,
            string message,
            string status,
            string systemSource,
            int? userId = null,
            int? lessonId = null,
            int? assignmentId = null)
        {
            return new Log
            {
                Timestamp = DateTime.UtcNow,
                ActionType = actionType,
                Message = message,
                Status = status,
                SystemSource = systemSource,
                UserId = userId,
                LessonId = lessonId,
                AssignmentId = assignmentId
            };
        }
    }
}

