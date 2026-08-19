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
        bool IsSample = false
    );
}
