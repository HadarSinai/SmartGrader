using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);

        /// <summary>
        /// חיפוש לפי מייל — נקודת הכניסה של שחזור הסיסמה. מנרמלת בדיוק כמו
        /// <see cref="GetByUsernameAsync"/>, אחרת "A@b.com" בטופס לא מוצא את "a@b.com" בשורה.
        /// </summary>
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// כל המשתמשות בתפקיד מסוים — מסך "מורות" של המנהלת.
        /// </summary>
        Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);

        /// <summary>
        /// ⚠️ <paramref name="excludingUserId"/> אינו קישוט: בעריכת מורה בלי שינוי המייל,
        /// בלעדיו היא מתנגשת עם עצמה ומקבלת 409.
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email, int? excludingUserId, CancellationToken ct = default);

        Task AddAsync(User user, CancellationToken ct = default);

        // GetByUsernameAsync מחזירה ישות מנותקת (AsNoTracking), ולכן עדכון מונה הכישלונות
        // ב-LoginHandler חייב לעבור כאן — בלי זה השינוי פשוט לא נשמר והנעילה לא קורית לעולם.
        Task UpdateAsync(User user, CancellationToken ct = default);
        Task DeleteAsync(User user, CancellationToken ct = default);
    }
}
