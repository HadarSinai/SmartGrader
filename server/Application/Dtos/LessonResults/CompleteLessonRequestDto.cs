namespace SmartGrader.Application.Dtos.LessonResults
{
    public class CompleteLessonRequestDto
    {
        public int StudentId { get; set; }
        public int LessonId { get; set; }

        /// <summary>
        /// <b>בקשת דריסה, לא הציון.</b> הציון הסופי נגזר בשרת מההגשות בשיעור; השדה הזה
        /// נכנס לתוקף רק כשהוא שונה מהמחושב, ואז <see cref="OverrideReason"/> הוא חובה.
        /// <para>
        /// <c>null</c> = "קבעי את מה שחושב" — הזרימה הרגילה.
        /// </para>
        /// </summary>
        public double? FinalScore { get; set; }

        /// <summary>הסיבה לדריסה. חובה כשהציון שהוזן שונה מהמחושב — היא יומן הביקורת.</summary>
        public string? OverrideReason { get; set; }

        // ⚠️ HasBonus הוסר: הוא הגיע מהלקוח וקבע את תקרת הציון (150 במקום 100). התקרה
        // נגזרת עכשיו מהתרגילים בפועל בשיעור.
    }
}
