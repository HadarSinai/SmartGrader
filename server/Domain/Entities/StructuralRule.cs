namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// דרישה מבנית אחת על הקוד של התלמידה — "חובה רקורסיה", "לכל היותר 3 if".
    /// <para>
    /// נבדקת ב-Roslyn (מנתח התחביר של C#) ולעולם לא במודל שפה: אותו קוד חייב לקבל את אותו
    /// ציון בכל הרצה. נשמרת כ-JSON על התרגיל ב-<c>Assignment.StructuralRulesJson</c>,
    /// באותה תבנית בדיוק כמו <c>TestsJson</c>.
    /// </para>
    /// </summary>
    public class StructuralRule
    {
        /// <summary>סוג הבדיקה: חובה / אסור / לפחות / לכל היותר.</summary>
        public RuleKind Kind { get; set; }

        /// <summary>המבנה הנבדק — לולאה, רקורסיה, מטריצה, מחלקה…</summary>
        public CodeConstruct Construct { get; set; }

        /// <summary>
        /// הסף ל-<see cref="RuleKind.AtLeast"/> / <see cref="RuleKind.AtMost"/>.
        /// חסר משמעות עבור MustUse/MustNotUse.
        /// </summary>
        public int Threshold { get; set; }

        public RuleSeverity Severity { get; set; }

        /// <summary>
        /// נקודות — רק ל-<see cref="RuleSeverity.Scored"/>. דרישה חוסמת היא שער ולא נושאת
        /// נקודות, והמלצה אינה משפיעה על הציון.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// ספירת המופעים שהדרישה מצפה לה, לפי סוגה. שימושי גם לניסוח ההסבר לתלמידה.
        /// </summary>
        public int ExpectedCount => Kind switch
        {
            RuleKind.MustUse => 1,
            RuleKind.MustNotUse => 0,
            _ => Threshold
        };

        /// <summary>
        /// האם ספירת מופעים בפועל מקיימת את הדרישה. הפונקציה היחידה שמכריעה זאת —
        /// גם המנתח וגם הטסטים עוברים דרכה, כדי שלא תהיה פרשנות שנייה לכלל.
        /// </summary>
        public bool IsSatisfiedBy(int actualCount) => Kind switch
        {
            RuleKind.MustUse => actualCount >= 1,
            RuleKind.MustNotUse => actualCount == 0,
            RuleKind.AtLeast => actualCount >= Threshold,
            RuleKind.AtMost => actualCount <= Threshold,
            _ => true
        };
    }
}
