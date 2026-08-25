namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// קישור חד-פעמי לאיפוס סיסמה. השורה מחזיקה <b>גיבוב</b> של הטוקן ולא את הטוקן עצמו —
    /// מי שמשיגה גישה לקריאה למסד הנתונים לא יכולה להתחזות לאף אחת, בדיוק כמו בסיסמאות.
    /// הטוקן הגולמי קיים רק בקישור שנשלח במייל.
    /// </summary>
    public class PasswordResetToken
    {
        /// <summary>תוקף הקישור. שעה — מספיק כדי לפתוח מייל, קצר מכדי להיות שימושי לתוקף.</summary>
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

        public int Id { get; private set; }
        public int UserId { get; private set; }
        public string TokenHash { get; private set; } = "";
        public DateTime ExpiresAt { get; private set; }

        /// <summary>
        /// מתי הטוקן הפסיק להיות שמיש. נחתם גם כשמישהי השתמשה בו בפועל וגם כשקישור חדש
        /// גבר עליו — לשני המקרים אותה משמעות בבדיקה, ולכן אין כאן שני שדות.
        /// </summary>
        public DateTime? UsedAt { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        protected PasswordResetToken() { }

        public static PasswordResetToken Create(int userId, string tokenHash, DateTime utcNow)
        {
            return new PasswordResetToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = utcNow.Add(Lifetime),
                CreatedAt = utcNow
            };
        }

        /// <summary>האם הטוקן עדיין שמיש: לא נוצל, לא גבר עליו קישור חדש, ולא פג.</summary>
        public bool IsUsable(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;

        /// <summary>
        /// סוגרת את הטוקן. משמשת גם למימוש וגם לביטול קישור ישן כשמתבקש קישור חדש.
        /// ⚠️ אינה דורסת חותמת קיימת — טוקן שכבר נסגר שומר על מועד הסגירה המקורי.
        /// </summary>
        public void MarkUsed(DateTime utcNow)
        {
            UsedAt ??= utcNow;
        }
    }
}
