namespace SmartGrader.Application.Dtos.LessonResults
{
    /// <summary>
    /// הקלט לדיאלוג "סיום שיעור": הציון שכל תרגיל קיבל, והממוצע כהצעה.
    /// <para>
    /// 🔴 <b>הפער שזה סוגר:</b> המערכת חישבה ציון לכל הגשה, והוא נעצר בהגשה ולא הגיע לעולם
    /// לציון השיעור. <c>CompleteLessonHandler</c> קיבל את <c>FinalScore</c> כלשונו ממה
    /// שהמורה הקלידה, והמסך פתח את הדיאלוג עם <c>null</c>. כלומר המורה חישבה ממוצע ביד,
    /// לכל תלמידה, בזמן שכל המספרים כבר היו במערכת.
    /// </para>
    /// </summary>
    public class LessonScoreSuggestionDto
    {
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public string? StudentName { get; set; }

        /// <summary>ציון לכל תרגיל בשיעור.</summary>
        public List<AssignmentScoreDto> AssignmentScores { get; set; } = new();

        /// <summary>
        /// הממוצע על התרגילים שיש להם ציון — <b>הצעה בלבד, ניתנת לעריכה</b>. המורה עדיין
        /// מחליטה, וגם הזנה ידנית לגמרי חייבת להישאר אפשרית: ר' ההערה ב-CompleteLessonHandler
        /// על מתן ציון ידני כשה-AI נכשל.
        /// <para><c>null</c> כשאף תרגיל לא נבדק — אין ממה לחשב.</para>
        /// </summary>
        public double? SuggestedScore { get; set; }

        /// <summary>כמה תרגילים נכללו בממוצע.</summary>
        public int GradedCount { get; set; }

        /// <summary>
        /// כמה תרגילים <b>לא</b> נכללו כי אין להם ציון. הדיאלוג חייב לומר זאת במפורש —
        /// ממוצע שמדלג על תרגיל בשקט נראה נכון ואינו נכון.
        /// </summary>
        public int UngradedCount { get; set; }

        /// <summary>
        /// יש בשיעור תרגיל בונוס, ולכן הציון הסופי רשאי לעבור 100.
        /// </summary>
        public bool HasBonus { get; set; }
    }

    public class AssignmentScoreDto
    {
        public int AssignmentId { get; set; }
        public string? Title { get; set; }

        /// <summary><c>null</c> = לא נבדק, ולכן אינו נכנס לממוצע.</summary>
        public double? Score { get; set; }

        /// <summary>סטטוס ההגשה, כדי שהדיאלוג יסביר <i>למה</i> אין ציון.</summary>
        public string Status { get; set; } = "לא הוגש";

        public bool IsBonus { get; set; }

        /// <summary>תקרת הציון של התרגיל — 100, או יותר בתרגיל בונוס.</summary>
        public int MaxScore { get; set; }
    }
}
