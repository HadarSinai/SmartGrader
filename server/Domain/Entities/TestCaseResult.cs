namespace SmartGrader.Domain.Entities
{
    // IsSample נשמר על התוצאה עצמה ולא נגזר מהתרגיל בזמן הקריאה: המורה יכולה לערוך את מקרי
    // הבדיקה אחרי שההגשה כבר נבדקה, ואז התאמה לפי אינדקס מול TestCase הנוכחי הייתה חושפת
    // תוצאה של מקרה מוסתר. ברירת המחדל false שומרת על fail closed גם לתוצאות ישנות ב-JSON.
    public sealed record TestCaseResult(
        string Input,
        string Expected,
        string Actual,
        bool Passed,
        string? Error,
        bool IsSample = false,
        // תיאור הסטטוס מ-Judge0 ("Wrong Answer" מול "Runtime Error (SIGSEGV)"). בלעדיו תשובה
        // שגויה וקריסה נראו זהות לגמרי במסך — שתיהן סתם "נכשל".
        string? StatusDescription = null,
        // נשמר על התוצאה מאותו נימוק כמו IsSample: ScoreCalculator שואל "האם כל מקרי הליבה
        // עברו", ואם הסיווג היה נקרא מהתרגיל בזמן החישוב, עריכת מקרי הבדיקה אחרי בדיקה
        // הייתה משנה למפרע ציון שכבר ניתן. ברירת המחדל true תואמת ל-TestCase.IsCore.
        bool IsCore = true
    );
}
