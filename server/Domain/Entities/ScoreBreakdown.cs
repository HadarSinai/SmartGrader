namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// פירוק הציון לרכיביו — מה שמוצג לתלמידה ולמורה במקום מספר בודד:
    /// <c>בדיקות 64 · דרישות 0 · סה"כ 64</c>.
    /// </summary>
    /// <param name="TestPoints">הנקודות שהתקבלו על מקרי הבדיקה, מתוך <paramref name="TestsAllocation"/>.</param>
    /// <param name="RulePoints">סכום הנקודות של הדרישות המנוקדות שהתקיימו.</param>
    /// <param name="Total">הציון הסופי — סכום השניים, מעוגל לספרה אחת.</param>
    /// <param name="TestsAllocation">כמה נקודות הוקצו למקרי הבדיקה מלכתחילה.</param>
    /// <param name="RulesAllocation">כמה נקודות הוקצו לדרישות מלכתחילה.</param>
    /// <param name="PassedTests">כמה מקרי בדיקה עברו.</param>
    /// <param name="TotalTests">כמה מקרי בדיקה רצו.</param>
    /// <param name="AllCorePassed">
    /// האם כל מקרי הליבה עברו. <c>false</c> מאפס את נקודות הטסטים — ר' ScoreCalculator.
    /// </param>
    public sealed record ScoreBreakdown(
        double TestPoints,
        double RulePoints,
        double Total,
        int TestsAllocation,
        int RulesAllocation,
        int PassedTests,
        int TotalTests,
        bool AllCorePassed);
}
