using FluentAssertions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// נעילת חשבון אחרי כישלונות רצופים — הבקרה המדויקת של ההתחברות (מגבלת ה-IP היא
    /// גסה בכוונה). באג כאן הוא או נעילה שלא נכנסת, או מורה שנעולה בלי סיבה.
    /// </summary>
    public class UserLockoutTests
    {
        private static readonly DateTime Now = new(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);

        private static User NewUser() =>
            User.Create("dana", "hash", "דנה כהן", UserRole.Teacher, "dana@school.org");

        /// <summary>רושם בדיוק את מספר הכישלונות שנועל — צמוד לקבוע, לא למספר קשיח.</summary>
        private static void FailUntilLocked(User user, DateTime at)
        {
            for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
                user.RegisterFailedLogin(at);
        }

        // ארבעה כישלונות — עדיין לא נעול
        [Fact]
        public void IsLockedOut_IsFalse_BeforeThreshold()
        {
            var user = NewUser();

            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);

            user.IsLockedOut(Now).Should().BeFalse();
        }

        // הכישלון החמישי נועל ל-15 דקות
        [Fact]
        public void IsLockedOut_IsTrue_AtMaxFailedAttempts()
        {
            var user = NewUser();

            FailUntilLocked(user, Now);

            user.IsLockedOut(Now).Should().BeTrue();
            user.LockoutEndsAt.Should().Be(Now.Add(User.LockoutDuration));
        }

        // הנעילה משתחררת בתום 15 הדקות — בדיוק על הגבול כבר לא נעול
        [Fact]
        public void IsLockedOut_IsFalse_AfterLockoutElapses()
        {
            var user = NewUser();
            FailUntilLocked(user, Now);

            user.IsLockedOut(Now.Add(User.LockoutDuration)).Should().BeFalse();
        }

        // כניסה מוצלחת מאפסת את המונה — ארבע טעויות ואז הצלחה לא משאירות חוב
        [Fact]
        public void RegisterSuccessfulLogin_ResetsCounter()
        {
            var user = NewUser();
            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);
            user.RegisterFailedLogin(Now);

            user.RegisterSuccessfulLogin();

            user.FailedLoginAttempts.Should().Be(0);
            user.LockoutEndsAt.Should().BeNull();
        }

        // נעילה שפגה מאפסת את המונה — כישלון חדש אחריה הוא 1, לא 6 (טעויות מפוזרות לא מצטברות)
        [Fact]
        public void RegisterFailedLogin_ResetsCounter_AfterExpiredLockout()
        {
            var user = NewUser();
            FailUntilLocked(user, Now);
            var afterLockout = Now.Add(User.LockoutDuration).AddMinutes(1);

            user.RegisterFailedLogin(afterLockout);

            user.FailedLoginAttempts.Should().Be(1);
            user.IsLockedOut(afterLockout).Should().BeFalse();
        }

        // שם משתמש ומייל מנורמלים ביצירה — האינדקס הייחודי לא יראה "A@b.com" ו-"a@b.com" כשניים
        [Fact]
        public void Create_NormalizesUsernameAndEmail()
        {
            var user = User.Create("  Dana ", "hash", " דנה ", UserRole.Teacher, " Dana@School.ORG ");

            user.Username.Should().Be("dana");
            user.Email.Should().Be("dana@school.org");
            user.FullName.Should().Be("דנה");
        }

        // מייל ריק הופך ל-null, לא למחרוזת ריקה
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetEmail_NormalizesEmptyToNull(string? email)
        {
            var user = NewUser();

            user.SetEmail(email);

            user.Email.Should().BeNull();
        }
    }
}
