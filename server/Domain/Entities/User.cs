namespace SmartGrader.Domain.Entities
{
    public enum UserRole
    {
        Teacher = 0,
        Student = 1,
        Admin = 2
    }

    public class User
    {
        /// <summary>
        /// כמה כישלונות רצופים נועלים חשבון.
        /// <para>
        /// ⚠️ הגבלת הקצב לפי IP (מדיניות "auth" ב-<c>Program.cs</c>) אינה מחליפה את זה: היא
        /// מונה בקשות ולא כישלונות, ותוקף שמנחש חשבון אחד מקבל דרכה מכסה מתחדשת ללא הגבלה —
        /// כ-7,200 ניחושים ביום. הנעילה כאן היא הבקרה המדויקת, והיא גם היחידה שאינה פוגעת
        /// במורות אחרות מאחורי אותו NAT.
        /// </para>
        /// </summary>
        public const int MaxFailedLoginAttempts = 5;

        /// <summary>משך הנעילה אחרי חציית הסף.</summary>
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public int Id { get; private set; }
        public string Username { get; private set; } = "";
        public string PasswordHash { get; private set; } = "";
        public string FullName { get; private set; } = "";

        /// <summary>
        /// כתובת המייל של המשתמשת. אופציונלית בסכימה בכוונה: לתלמידות אין מייל, וגם לכל
        /// השורות שנוצרו לפני העמודה הזו אין. החובה על מורות נאכפת ב-validator של
        /// CreateTeacher/UpdateTeacher, לא ברמת ה-DB.
        /// </summary>
        public string? Email { get; private set; }

        public UserRole Role { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        // ── נעילת חשבון אחרי כישלונות רצופים ──
        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockoutEndsAt { get; private set; }

        protected User() { }

        public static User Create(
            string username,
            string passwordHash,
            string fullName,
            UserRole role,
            string? email = null)
        {
            return new User
            {
                Username = username.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                FullName = fullName.Trim(),
                Email = NormalizeEmail(email),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public void SetFullName(string fullName)
        {
            FullName = fullName.Trim();
        }

        /// <summary>
        /// ⚠️ אין <c>SetUsername</c> במכוון — שם המשתמש הוא מזהה ההתחברות והוא בלתי-משתנה.
        /// המייל הוא מה שמשתנה, והוא מנורמל בדיוק כמו שם המשתמש ב-<see cref="Create"/>,
        /// כדי שהאינדקס הייחודי לא יראה "A@b.com" ו-"a@b.com" כשתי כתובות שונות.
        /// </summary>
        public void SetEmail(string? email)
        {
            Email = NormalizeEmail(email);
        }

        private static string? NormalizeEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        /// <summary>האם החשבון נעול כרגע.</summary>
        public bool IsLockedOut(DateTime utcNow) =>
            LockoutEndsAt.HasValue && LockoutEndsAt.Value > utcNow;

        /// <summary>
        /// רושמת ניסיון כניסה כושל, ונועלת את החשבון בחצייתה של
        /// <see cref="MaxFailedLoginAttempts"/>.
        /// </summary>
        /// <remarks>
        /// ⚠️ אין לקרוא לזה כשהחשבון כבר נעול. תוקף שממשיך לנחש היה מאריך את הנעילה
        /// בכל ניסיון והופך אותה לנעילה קבועה של חשבון שאינו שלו —
        /// <c>LoginHandler</c> יוצא לפני כן.
        /// </remarks>
        public void RegisterFailedLogin(DateTime utcNow)
        {
            // נעילה שפגה מאפסת את המונה: בלי זה חמישה כישלונות מפוזרים על פני חודש —
            // כלומר טעויות הקלדה רגילות — היו מצטברים לנעילה.
            if (LockoutEndsAt.HasValue && LockoutEndsAt.Value <= utcNow)
            {
                FailedLoginAttempts = 0;
                LockoutEndsAt = null;
            }

            FailedLoginAttempts++;

            if (FailedLoginAttempts >= MaxFailedLoginAttempts)
                LockoutEndsAt = utcNow.Add(LockoutDuration);
        }

        /// <summary>מאפסת את המונה אחרי כניסה מוצלחת.</summary>
        public void RegisterSuccessfulLogin()
        {
            FailedLoginAttempts = 0;
            LockoutEndsAt = null;
        }
    }
}
