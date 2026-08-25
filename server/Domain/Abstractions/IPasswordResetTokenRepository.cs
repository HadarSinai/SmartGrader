using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface IPasswordResetTokenRepository
    {
        /// <summary>
        /// ⚠️ מחזירה ישות <b>נעקבת</b> (בלי AsNoTracking), בשונה מרוב שאילתות הקריאה:
        /// <c>ResetPasswordHandler</c> חותם עליה <c>UsedAt</c> מיד אחרי הקריאה. בלי מעקב
        /// החותמת לא הייתה נשמרת, והקישור היה נשאר שמיש לשימוש חוזר — כלומר בדיוק הבאג
        /// שהחד-פעמיות נועדה למנוע.
        /// </summary>
        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

        Task AddAsync(PasswordResetToken token, CancellationToken ct = default);

        /// <summary>
        /// סוגרת כל קישור פתוח של המשתמשת, כך שבקשה חדשה גוברת על קודמותיה.
        /// ⚠️ אינה קוראת ל-<c>SaveChangesAsync</c> — השמירה היא של ה-handler דרך IUnitOfWork.
        /// </summary>
        Task InvalidateAllForUserAsync(int userId, DateTime utcNow, CancellationToken ct = default);
    }
}
