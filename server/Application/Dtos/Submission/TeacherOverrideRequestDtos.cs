namespace SmartGrader.Application.Dtos.Submissions
{
    /// <summary>
    /// אישור המורה להגשה נוספת.
    /// <para>
    /// ⚠️ אין כאן מזהה מורה בכוונה — הוא נלקח מה-claims של הטוקן בבקר. שדה בגוף הבקשה
    /// היה הופך את יומן הביקורת לדיווח עצמי, כלומר לחסר ערך.
    /// </para>
    /// </summary>
    public class GrantExtraAttemptRequestDto
    {
        /// <summary>הסיבה — חובה. זה מה שמחליף את "לראות מי השתמשה בקוד".</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>דריסת ציון בידי המורה.</summary>
    public class OverrideScoreRequestDto
    {
        /// <summary>
        /// הציון החדש, תמיד <c>0..100</c> — גם בתרגיל בונוס, שכן הבונוס מוסיף לציון
        /// השיעור ואינו מרים את תקרת התרגיל (<c>Assignment.TotalPoints</c>).
        /// </summary>
        public double Score { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
