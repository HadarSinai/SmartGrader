using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Validation
{
    /// <summary>
    /// תרגיל שאין בו במה לנקד, והרובריקה שלא מסתכמת ב-100.
    /// <para>
    /// בלי הכלל הראשון תרגיל בלי אף מקרה בדיקה נשמר בשקט, ואז הציון מחושב על
    /// <c>Total == 0</c> ונותן <b>0 לכל התלמידות</b> בלי שאף אחת עשתה משהו לא בסדר.
    /// </para>
    /// </summary>
    public static class AssignmentGradeability
    {
        public const string Message =
            "לתרגיל חייב להיות לפחות מקרה בדיקה אחד או לפחות דרישה מבנית אחת — " +
            "אחרת אי אפשר לנקד אותו וכל התלמידות יקבלו 0.";

        /// <summary>
        /// ⚠️ 100 בכל תרגיל, בונוס או לא: הבונוס אינו מרים את תקרת התרגיל אלא מוסיף לציון
        /// השיעור — ר' <c>Assignment.TotalPoints</c> ו-<c>LessonScoreCalculator</c>.
        /// </summary>
        public static readonly string RubricMessage =
            $"הניקוד חייב להסתכם ב-{Assignment.TotalPoints} בדיוק: בדיקות + סכום הנקודות של הדרישות המנוקדות.";

        public const string ScoredPointsMessage =
            "דרישה מנוקדת חייבת לשאת לפחות נקודה אחת. דרישה חוסמת והמלצה אינן נושאות ניקוד.";

        /// <summary>
        /// ⚠️ <b>"או", לא "וגם".</b> תרגיל מחלקות — "כתבי מחלקה Student עם בנאי ושתי תכונות" —
        /// אין לו קלט, אין לו פלט ואין מה להריץ בו; הוא מנוקד על המבנה בלבד. רק תרגיל
        /// שאין לו לא זה ולא זה אינו חוקי.
        /// </summary>
        public static bool IsGradeable(
            IReadOnlyCollection<TestCaseDto>? tests,
            IReadOnlyCollection<StructuralRuleDto>? rules) =>
            tests is { Count: > 0 } || rules is { Count: > 0 };

        /// <summary>
        /// הרובריקה מסתכמת ב-100 בדיוק, כמו במחוון בחינה.
        /// <para>
        /// <b>אין דרישות מנוקדות? הבדיקות מקבלות את כל 100 אוטומטית</b>, ולכן תרגיל רגיל
        /// נשאר מהיר ליצירה בדיוק כמו קודם.
        /// </para>
        /// <para>
        /// ⚠️ המקרה שקל לפספס: תרגיל <b>בלי טסטים ובלי דרישות מנוקדות</b> — רק חוסמות, הצורה
        /// הטבעית של תרגיל מחלקות. אין לְמה להקצות נקודות, והוא חוקי:
        /// <c>ScoreCalculator</c> נותן 100 לתלמידה שקיימה כל שער. הכלל כאן חייב לתת לו לעבור.
        /// </para>
        /// </summary>
        public static bool HasValidRubric(
            int testsAllocation,
            IReadOnlyCollection<TestCaseDto>? tests,
            IReadOnlyCollection<StructuralRuleDto>? rules)
        {
            const int maxScore = Assignment.TotalPoints;

            if (testsAllocation < 0 || testsAllocation > maxScore)
                return false;

            var rulesAllocation = ScoredRules(rules).Sum(r => r.Points);
            var hasTests = tests is { Count: > 0 };

            // אין למה להקצות נקודות בכלל — מותר, ר' ההערה למעלה.
            if (!hasTests && rulesAllocation == 0)
                return true;

            // יש טסטים אבל הוקצו להם 0 נקודות והדרישות אינן מכסות את היתר: זו טעות הקלדה,
            // לא בחירה — התלמידה תריץ בדיקות שאינן שוות דבר.
            var effectiveTestsAllocation = hasTests ? testsAllocation : 0;

            return effectiveTestsAllocation + rulesAllocation == maxScore;
        }

        /// <summary>דרישה מנוקדת בלי נקודות אינה עושה כלום — כנראה נשכח למלא את השדה.</summary>
        public static bool ScoredRulesCarryPoints(IReadOnlyCollection<StructuralRuleDto>? rules) =>
            ScoredRules(rules).All(r => r.Points > 0);

        private static IEnumerable<StructuralRuleDto> ScoredRules(
            IReadOnlyCollection<StructuralRuleDto>? rules) =>
            (rules ?? Array.Empty<StructuralRuleDto>())
                .Where(r => string.Equals(r.Severity, nameof(RuleSeverity.Scored), StringComparison.OrdinalIgnoreCase));
    }
}
