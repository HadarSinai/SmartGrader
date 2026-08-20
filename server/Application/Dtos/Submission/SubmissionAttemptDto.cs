namespace SmartGrader.Application.Dtos.Submissions
{
    /// <summary>
    /// ניסיון הגשה קודם — שורה בציר "ניסיון 1: 40 · ניסיון 2: 78".
    /// <para>
    /// ⚠️ <b>רק הניסיון האחרון נחשב כציון.</b> השורות כאן אינן נכנסות לשום ממוצע — הן קיימות
    /// כדי שהגשה חוזרת לא תמחק את ההיסטוריה, שעד כה נדרסה במקום בכל ניסיון.
    /// </para>
    /// </summary>
    public class SubmissionAttemptDto
    {
        public int AttemptNumber { get; set; }
        public double? Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }

        /// <summary>
        /// התוכן הכבד (קוד, משוב, תוצאות) נגזם. ניסיונות בלתי מוגבלים עם ארכיון מלא גדלים
        /// בלי חסם ב-SQLite; נשמרים 10 האחרונים במלואם, והשאר מצטמצמים לציון וחותמת זמן.
        /// </summary>
        public bool IsCollapsed { get; set; }
    }
}
