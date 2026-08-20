namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// תוצאת בדיקה של דרישה מבנית אחת מול קוד אחד. נשמרת על ההגשה
    /// (<c>Submission.StructuralResultsJson</c>) ומוצגת למורה ולתלמידה.
    /// </summary>
    public class StructuralRuleResult
    {
        /// <summary>הדרישה שנבדקה — נשמרת יחד עם התוצאה כדי שההגשה תתאר את עצמה גם
        /// אחרי שהמורה תשנה את הדרישות בתרגיל.</summary>
        public StructuralRule Rule { get; set; } = new();

        public bool Passed { get; set; }

        /// <summary>כמה מופעים נמצאו בפועל. עבור <see cref="CodeConstruct.NestedLoopDepth"/>
        /// זהו עומק הקינון ולא ספירה.</summary>
        public int ActualCount { get; set; }

        /// <summary>
        /// מספרי השורות שבהן נמצאו המופעים (1-based), כדי שהמשוב יוכל לומר "בשורה 12".
        /// ריק כשלא נמצא דבר.
        /// </summary>
        public List<int> Lines { get; set; } = new();
    }
}
