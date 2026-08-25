namespace SmartGrader.Application.Common.Interfaces
{
    /// <summary>
    /// Sends notification emails to the system admin.
    /// Implementations must be best-effort: never throw when SMTP is not configured.
    /// </summary>
    public interface IEmailSender
    {
        Task SendToAdminAsync(string subject, string body, CancellationToken ct = default);

        /// <summary>
        /// שולחת לכתובת מפורשת. נוספה עבור שחזור סיסמה — עד כאן היעד היחיד היה המנהלת.
        /// <para>
        /// ⚠️ מחזירה <c>false</c> כאשר SMTP אינו מוגדר, במקום להשתיק כמו
        /// <see cref="SendToAdminAsync"/>. ההבדל אינו קוסמטי: התראת שגיאה שלא נשלחה היא
        /// אי-נוחות, אבל קישור איפוס שלא נשלח משאיר מורה מול הודעת "נשלח אליך קישור"
        /// שלעולם לא יגיע. הקורא חייב לדעת כדי לרשום את זה כתקלה תפעולית.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> אם המייל נשלח בפועל.</returns>
        Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);
    }

    /// <summary>
    /// כתובת הבסיס של הלקוח (App:ClientBaseUrl). השרת בונה קישורים לתוך הלקוח — למשל
    /// קישור איפוס הסיסמה — ואין לו דרך להסיק אותה מבקשת ה-HTTP: הבקשה שממנה נשלח המייל
    /// היא בקשת API, וכתובת ה-Referer של דפדפן אינה מקור שאפשר לסמוך עליו.
    /// </summary>
    public interface IClientUrlProvider
    {
        /// <summary>כתובת הבסיס בלי לוכסן מסיים, או מחרוזת ריקה כשאינה מוגדרת.</summary>
        string BaseUrl { get; }

        bool IsConfigured { get; }
    }
}
