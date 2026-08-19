using SmartGrader.Application.Dtos.Assignments;

namespace SmartGrader.Application.Common.Validation
{
    /// <summary>
    /// תרגיל שאין בו במה לנקד. בלי הכלל הזה תרגיל בלי אף מקרה בדיקה נשמר בשקט, ואז
    /// <c>AiWorker</c> מחשב <c>Total == 0</c> ונותן <b>0 לכל התלמידות</b> בלי שאף אחת עשתה משהו לא בסדר.
    /// </summary>
    public static class AssignmentGradeability
    {
        public const string Message =
            "לתרגיל חייב להיות לפחות מקרה בדיקה אחד — אחרת אי אפשר לנקד אותו וכל התלמידות יקבלו 0.";

        /// <summary>
        /// ⚠️ נקודת ההרחבה: כשייכנסו דרישות מבניות (תרגיל מחלקות — "כתבי מחלקה Student עם בנאי
        /// ושתי תכונות" — אין לו קלט ואין לו פלט, והוא מנוקד על המבנה בלבד), התנאי הופך ל-
        /// <c>tests is { Count: &gt; 0 } || requirements is { Count: &gt; 0 }</c>. שינוי שורה אחת כאן,
        /// בלי לגעת באף Validator.
        /// </summary>
        public static bool IsGradeable(IReadOnlyCollection<TestCaseDto>? tests) =>
            tests is { Count: > 0 };
    }
}
