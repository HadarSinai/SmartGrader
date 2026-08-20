namespace SmartGrader.Application.Dtos.Submissions
{
    /// <summary>
    /// המשוב המילולי כפי שהוא חוזר ללקוח.
    /// <para>
    /// 🔴 <b>אין כאן ציונים.</b> השדה <c>Scores</c> (ארבעה מספרים שהמודל ניקד בעצמו) הוסר:
    /// את הציון קובעים Roslyn, מריץ הקוד ו-<c>ScoreCalculator</c>, והפירוק האמיתי חוזר
    /// ב-<see cref="SubmissionResponseDto.ScoreBreakdown"/>.
    /// </para>
    /// </summary>
    public class AiFeedbackResultDto
    {
        public List<string> Good { get; set; } = new();
        public AiFeedbackIssuesDto Issues { get; set; } = new();
        public List<string> MinimalChanges { get; set; } = new();

        // כאשר false, RawResponse מכיל את טקסט המשוב הגולמי (לא פורש) לתצוגת גיבוי בקליינט.
        public bool ParseSucceeded { get; set; } = true;
        public string? RawResponse { get; set; }
    }

    public class AiFeedbackIssuesDto
    {
        public List<string> Correctness { get; set; } = new();
        public List<string> Readability { get; set; } = new();
        public List<string> Performance { get; set; } = new();
    }
}
