using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.CodeAnalysis;

/// <summary>
/// בודק את הדרישות המבניות של התרגיל מול הקוד של התלמידה.
/// <para>
/// ⚠️ <b>הבדיקה חייבת להיות דטרמיניסטית.</b> המימוש הוא מנתח התחביר של C# (Roslyn) ולעולם
/// לא מודל שפה: אותו קוד מקבל את אותו ציון בכל הרצה, וזו כל הסיבה שהדרישות רשאיות לשאת
/// נקודות. מודל שפה נוגע רק בניסוח ההסבר בעברית — ר' <c>IFeedbackService</c>.
/// </para>
/// </summary>
public interface ICodeAnalysisService
{
    /// <summary>
    /// מריץ את כל הדרישות על הקוד ומחזיר תוצאה אחת לכל דרישה, באותו סדר.
    /// </summary>
    /// <param name="sourceCode">
    /// הקוד לניתוח. במסלול רב-קובצי זהו חיבור הקבצים — מספרי השורות מתייחסים לטקסט
    /// המחובר ולא לקובץ בודד.
    /// </param>
    /// <param name="rules">הדרישות מהתרגיל. רשימה ריקה מחזירה רשימה ריקה.</param>
    /// <remarks>
    /// לעולם אינו זורק. קוד שאינו ניתן לפירוש מחזיר <see cref="CodeAnalysisResult.HasSyntaxErrors"/>
    /// ולא חריגה — כשל בניתוח אסור שיפיל בדיקה של הגשה.
    /// </remarks>
    CodeAnalysisResult Analyze(string? sourceCode, IReadOnlyList<StructuralRule>? rules);
}

/// <summary>
/// תוצאת הניתוח כולה.
/// </summary>
/// <param name="Results">תוצאה לכל דרישה, בסדר שבו הדרישות הגיעו.</param>
/// <param name="HasSyntaxErrors">
/// הקוד לא נפרש כ-C# תקין.
/// <para>
/// ⚠️ כשזה <c>true</c> <b>אין להפעיל את השער החוסם</b>: קוד עם נקודה-פסיק חסרה ייספר כמי
/// שאין בו רקורסיה, והתלמידה תקבל "התרגיל דרש רקורסיה" במקום שגיאת הקומפילציה האמיתית.
/// במקרה כזה ההגשה ממשיכה ל-Judge0, שידווח את השגיאה שבאמת קרתה.
/// </para>
/// </param>
public sealed record CodeAnalysisResult(
    IReadOnlyList<StructuralRuleResult> Results,
    bool HasSyntaxErrors)
{
    public static readonly CodeAnalysisResult Empty =
        new(Array.Empty<StructuralRuleResult>(), HasSyntaxErrors: false);

    /// <summary>
    /// הדרישות החוסמות שלא התקיימו. ריק כשאין מה לחסום.
    /// </summary>
    public IReadOnlyList<StructuralRuleResult> FailedBlockingRules =>
        Results
            .Where(r => !r.Passed && r.Rule.Severity == RuleSeverity.Blocking)
            .ToList();
}
