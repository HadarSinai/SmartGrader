namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// תמונת מצב של ניסיון הגשה שהסתיים, לפני שההגשה אופסה לניסיון הבא.
    /// <para>
    /// ⚠️ <b>טבלה נפרדת בכוונה.</b> יש אינדקס ייחודי על <c>(StudentId, AssignmentId)</c>, ולכן
    /// שמירת ניסיון כשורת <see cref="Submission"/> נוספת הייתה מפרה אותו מיד. שורת ההגשה
    /// היא תמיד המצב הנוכחי היחיד; ההיסטוריה יושבת כאן.
    /// </para>
    /// <para>
    /// <b>רק הניסיון האחרון נחשב כציון</b> — הרשומות כאן אינן נכנסות לשום ממוצע.
    /// </para>
    /// </summary>
    public class SubmissionAttempt
    {
        /// <summary>כמה ניסיונות נשמרים במלואם. מעבר לזה נשמרים רק ציון וחותמת זמן.</summary>
        public const int FullDetailRetentionCount = 10;

        public int Id { get; private set; }
        public int SubmissionId { get; private set; }

        /// <summary>מספר הניסיון, החל מ-1.</summary>
        public int AttemptNumber { get; private set; }

        public double? Score { get; private set; }
        public SubmissionStatus Status { get; private set; }

        public string? FeedbackJson { get; private set; }
        public string TestResultsJson { get; private set; } = "[]";
        public string StructuralResultsJson { get; private set; } = "[]";
        public string? ScoreBreakdownJson { get; private set; }

        public string SourceCode { get; private set; } = "";
        public string SourceFilesJson { get; private set; } = "[]";

        public string? CompileError { get; private set; }
        public string? AiError { get; private set; }

        public DateTime SubmittedAt { get; private set; }
        public DateTime? GradedAt { get; private set; }

        /// <summary>
        /// התוכן הכבד (קוד, משוב, תוצאות) נגזם. ניסיונות בלתי מוגבלים עם ארכיון מלא גדלים
        /// בלי חסם ב-SQLite, ולקוד ולמשוב של ניסיון 3 מתוך 30 אין קהל.
        /// </summary>
        public bool IsCollapsed { get; private set; }

        public Submission Submission { get; private set; } = null!;

        private SubmissionAttempt() { } // EF Core

        /// <summary>
        /// מצלם את מצבה הנוכחי של ההגשה. נקרא מתוך <see cref="Submission.MarkPendingAi"/>
        /// בלבד, רגע לפני האיפוס.
        /// </summary>
        internal static SubmissionAttempt CaptureFrom(Submission submission) => new()
        {
            SubmissionId = submission.Id,
            AttemptNumber = submission.AttemptNumber,
            Score = submission.Score,
            Status = submission.Status,
            FeedbackJson = submission.FeedbackJson,
            TestResultsJson = submission.TestResultsJson,
            StructuralResultsJson = submission.StructuralResultsJson,
            ScoreBreakdownJson = submission.ScoreBreakdownJson,
            SourceCode = submission.SourceCode,
            SourceFilesJson = submission.SourceFilesJson,
            CompileError = submission.CompileError,
            AiError = submission.AiError,
            SubmittedAt = submission.LastSubmittedAt,
            GradedAt = submission.GradedAt
        };

        /// <summary>גוזם את התוכן הכבד ומשאיר ציון, סטטוס וחותמת זמן.</summary>
        internal void Collapse()
        {
            if (IsCollapsed) return;

            FeedbackJson = null;
            TestResultsJson = "[]";
            StructuralResultsJson = "[]";
            ScoreBreakdownJson = null;
            SourceCode = "";
            SourceFilesJson = "[]";
            CompileError = null;
            AiError = null;
            IsCollapsed = true;
        }
    }
}
