using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.Feedback
{
    /// <summary>מקרה בדיקה כפי שהמודל הציע אותו — לפני שהורץ ולפני שאיש בדק אותו.</summary>
    public sealed record SuggestedTestCase(
        string Input,
        string Expected,
        string? Why,
        bool IsCore);

    /// <summary>
    /// המודל אינו זמין (חסר מפתח, שגיאת רשת, תשובה לא תקינה). נזרק ולא מוחזר כרשימה ריקה,
    /// כדי שההודעה למורה תהיה "ה-AI לא זמין" ולא "לא נמצאו הצעות" — שני מצבים שונים לגמרי.
    /// <para>
    /// ⚠️ הכשל הזה חייב להישאר <b>מבודד</b>: כתיבה ידנית של מקרי בדיקה ואימות מול הפתרון
    /// לדוגמה לא נוגעים במודל בכלל וממשיכים לעבוד גם כשהוא נופל.
    /// </para>
    /// </summary>
    public class TestCaseSuggestionUnavailableException : Exception
    {
        public TestCaseSuggestionUnavailableException(string message) : base(message) { }
    }

    public interface ITestCaseSuggestionService
    {
        /// <param name="count">מספר המקרים המבוקש. הקורא כבר תחם אותו — ר' SuggestTestCasesLimits.</param>
        Task<IReadOnlyList<SuggestedTestCase>> SuggestAsync(
            string description,
            GradingMode gradingMode,
            string? methodName,
            int count,
            CancellationToken ct);
    }
}
