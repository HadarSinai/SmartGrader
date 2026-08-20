using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Entities;

/// <summary>
/// כותב את המשוב המילולי בעברית.
/// <para>
/// כל העובדות הוכרעו לפני הקריאה לכאן: Roslyn קבע אם הדרישה התקיימה, מריץ הקוד קבע אם
/// הפלט תאם, ו-<c>ScoreCalculator</c> קבע את המספר. <b>המודל תורם רק את הניסוח</b> — ולכן
/// הציון נשאר משוחזר גם כשהמילים משתנות בין הרצות.
/// </para>
/// <para>
/// ⚠️ <b>שלושה תרחישים ולא פרומפט אחד.</b> פרומפט אחד נשא הוראות שלא חלות על המקרה —
/// שגיאת קומפילציה אינה זקוקה להוראות על טסטים או על רובריקה — ושילם עליהן טוקנים בכל
/// הגשה. הפיצול חוסך כמחצית מהקלט וגם מונע דליפה: המסלול של דרישה שלא התקיימה אינו מקבל
/// נתוני טסטים בכלל, כי Judge0 מעולם לא רץ.
/// </para>
/// </summary>
public interface IFeedbackService
{
    /// <summary>הסבר בעברית לשגיאת קומפילציה, במקום <c>error CS0103</c> גולמי.</summary>
    /// <param name="compilerMessage">
    /// פלט המהדר. ⚠️ הריצה היא ב-Mono (Judge0), שישן בהרבה מהמהדר שמנתח את הקוד כאן —
    /// המימוש מציין זאת בפרומפט כדי שתחביר מודרני שנדחה יזוהה בשמו.
    /// </param>
    Task<AiFeedbackResult> GetCompileErrorFeedbackAsync(
        string assignmentDescription,
        string sourceCode,
        string compilerMessage,
        CancellationToken ct);

    /// <summary>
    /// הסבר בעברית לדרישה חוסמת שלא התקיימה. <b>בלי שום נתוני טסטים</b> — במסלול הזה
    /// Judge0 אינו נקרא כלל, ואין מה למסור.
    /// </summary>
    Task<AiFeedbackResult> GetRequirementFeedbackAsync(
        string assignmentDescription,
        string sourceCode,
        IReadOnlyList<StructuralRuleResult> failedRules,
        CancellationToken ct);

    /// <summary>
    /// משוב למסלול הרגיל — אחרי שהטסטים רצו והציון חושב.
    /// </summary>
    /// <param name="testDetails">
    /// 🔴 המימוש מוסר למודל <b>רק מקרי דוגמה</b>. מודל שקיבל <c>expected: 10</c> יכתוב
    /// "החזרת 45 במקום 10" ובכך ידליף את התשובה — נתיב דלף זהה בפועל להחזרת המקרים ב-API,
    /// רק עקיף. הספירה (עברו/נכשלו) מספיקה כדי לבסס את המשוב.
    /// </param>
    Task<AiFeedbackResult> GetGradingFeedbackAsync(
        string assignmentDescription,
        string sourceCode,
        int passedTests,
        int totalTests,
        IReadOnlyList<TestCaseResult> testDetails,
        IReadOnlyList<StructuralRuleResult> ruleResults,
        CancellationToken ct);
}
