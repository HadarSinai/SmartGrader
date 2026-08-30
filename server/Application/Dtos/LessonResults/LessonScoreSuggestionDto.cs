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
        /// הממוצע על תרגילי החובה שנבדקו, לפני הבונוס. <c>null</c> כשאין אף אחד כזה.
        /// </summary>
        public double? BaseScore { get; set; }

        /// <summary>כמה נקודות תרגילי הבונוס הוסיפו בפועל. 0 כשאין בונוס או שלא הוגש.</summary>
        public double BonusPoints { get; set; }

        /// <summary>
        /// תקרת השיעור: <c>100 + Σ BonusValue</c>. ⚠️ נגזרת מהתרגילים בפועל — הדיאלוג
        /// חייב לקרוא אותה מכאן ולא לחשב אותה לעצמו.
        /// </summary>
        public double MaxScore { get; set; }
    }

    public class AssignmentScoreDto
    {
        public int AssignmentId { get; set; }
        public string? Title { get; set; }

        /// <summary><c>null</c> = לא נבדק, ולכן אינו נכנס לממוצע.</summary>
        public double? Score { get; set; }

        /// <summary>סטטוס ההגשה, כדי שהדיאלוג יסביר <i>למה</i> אין ציון.</summary>
        public string Status { get; set; } = "לא הוגש";

        /// <summary>
        /// תרגיל בונוס. הציון שלו הוא עדיין מתוך 100 — מה שמשתנה הוא שהוא אינו נכנס
        /// לממוצע אלא מוסיף <c>BonusValue × (הציון ÷ 100)</c> לציון השיעור.
        /// </summary>
        public bool IsBonus { get; set; }

        /// <summary>כמה נקודות התרגיל הזה מוסיף לשיעור כשהוא נעשה במלואו. 0 כשאינו בונוס.</summary>
        public double BonusValue { get; set; }
    }
}
