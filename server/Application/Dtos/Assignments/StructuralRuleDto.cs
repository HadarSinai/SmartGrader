namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// דרישה מבנית אחת בטופס התרגיל — "חובה להשתמש ברקורסיה", "לכל היותר 3 if".
    /// <para>
    /// ה-enum-ים עוברים כמחרוזות ולא כמספרים, בדיוק כמו <c>GradingMode</c>: הערך המספרי
    /// של <c>CodeConstruct</c> אינו חלק מהחוזה, והקטלוג גדל כל סמסטר.
    /// </para>
    /// </summary>
    public class StructuralRuleDto
    {
        /// <summary><c>MustUse</c> · <c>MustNotUse</c> · <c>AtLeast</c> · <c>AtMost</c>.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>שם ערך מתוך <c>CodeConstruct</c> — <c>While</c>, <c>Recursion</c>, <c>Matrix</c>…</summary>
        public string Construct { get; set; } = string.Empty;

        /// <summary>הסף ל-<c>AtLeast</c>/<c>AtMost</c>. חסר משמעות ל-<c>MustUse</c>/<c>MustNotUse</c>.</summary>
        public int Threshold { get; set; }

        /// <summary>
        /// <c>Blocking</c> (אין ציון כלל · הגשה חוזרת) · <c>Scored</c> (עולה נקודות) ·
        /// <c>Advisory</c> (הערה בלבד).
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>נקודות — רק ל-<c>Scored</c>. דרישה חוסמת היא שער ואינה נושאת ניקוד.</summary>
        public int Points { get; set; }
    }
}
