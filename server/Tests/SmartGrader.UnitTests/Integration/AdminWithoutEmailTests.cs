using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Repositories;
using Xunit;

namespace SmartGrader.UnitTests.Integration
{
    /// <summary>
    /// 🔴 שורת מנהלת בלי מייל היא החשבון היחיד שאין ממנו דרך חזרה: <c>GetByEmailAsync</c>
    /// משווה למחרוזת מנורמלת ולעולם אינה מתאימה ל-NULL, ואין מעל המנהלת מי שיאפס לה סיסמה.
    /// השאילתה הזו היא מה שמאתר את השורה, ואזהרת האתחול היא הסימן היחיד לקיומה
    /// (‏<c>B-50</c>).
    /// </summary>
    /// <remarks>
    /// ⚠️ מול מסד אמיתי ולא מול תחליף בזיכרון: השוואת <c>== null</c> נפתרת ל-SQL, וספק
    /// שמתרגם אותה אחרת הוא בדיוק סוג התקלה שתחליף בזיכרון מסתיר.
    /// </remarks>
    public class AdminWithoutEmailTests
    {
        private static User AddUser(SchoolDatabase db, UserRole role, string? email)
        {
            var user = User.Create($"user-{Guid.NewGuid():N}", "hash", "שם", role, email);

            db.Context.Users.Add(user);
            db.Context.SaveChanges();
            db.Context.ChangeTracker.Clear();
            return user;
        }

        // מנהלת בלי מייל — נמצאת
        [Fact]
        public async Task GetByRoleWithoutEmail_FindsAnAdminWithNoAddress()
        {
            using var db = new SchoolDatabase();
            var stranded = AddUser(db, UserRole.Admin, email: null);

            var found = await new UserRepository(db.Context)
                .GetByRoleWithoutEmailAsync(UserRole.Admin);

            found.Should().ContainSingle().Which.Id.Should().Be(stranded.Id);
        }

        /// <summary>
        /// מייל ריק ומייל NULL הם אותה תקלה מבחינת השחזור, ולכן שניהם נמצאים.
        /// </summary>
        /// <remarks>
        /// ⚠️ העמודה נכתבת ב-SQL ולא דרך הישות, וזה <b>לא</b> עקיפה של ה-API: <c>SetEmail</c>
        /// מנרמל מחרוזת ריקה ל-NULL, כך שדרכו אי אפשר להגיע למצב הזה כלל. שורה עם מחרוזת
        /// ריקה מגיעה ממקום אחר — גיבוי ישן, מיגרציה עם ברירת מחדל, או UPDATE ידני — וזה
        /// בדיוק המקרה שהכלל קיים בשבילו. בדיקה דרך הישות הייתה משכפלת את הטסט הקודם
        /// תחת שם שמבטיח משהו אחר.
        /// </remarks>
        [Fact]
        public async Task GetByRoleWithoutEmail_TreatsAnEmptyAddressAsNoAddress()
        {
            using var db = new SchoolDatabase();
            var admin = AddUser(db, UserRole.Admin, email: "admin@school.org");

            db.Context.Database.ExecuteSqlRaw(
                "UPDATE Users SET Email = '' WHERE Id = {0}", admin.Id);
            db.Context.ChangeTracker.Clear();

            var found = await new UserRepository(db.Context)
                .GetByRoleWithoutEmailAsync(UserRole.Admin);

            found.Should().ContainSingle().Which.Id.Should().Be(admin.Id);
        }

        // מנהלת עם מייל אינה נמצאת — אחרת האזהרה הייתה נדלקת בכל התקנה תקינה ומאבדת משמעות
        [Fact]
        public async Task GetByRoleWithoutEmail_IgnoresAnAdminThatHasAnAddress()
        {
            using var db = new SchoolDatabase();
            AddUser(db, UserRole.Admin, email: "admin@school.org");

            var found = await new UserRepository(db.Context)
                .GetByRoleWithoutEmailAsync(UserRole.Admin);

            found.Should().BeEmpty();
        }

        // ⚠️ מסונן לפי תפקיד: מורה בלי מייל אינה נעולה החוצה — מנהלת מאפסת לה סיסמה,
        // ולכן שורה כזו אינה מצדיקה את האזהרה
        [Fact]
        public async Task GetByRoleWithoutEmail_IgnoresATeacherWithNoAddress()
        {
            using var db = new SchoolDatabase();
            AddUser(db, UserRole.Teacher, email: null);

            var found = await new UserRepository(db.Context)
                .GetByRoleWithoutEmailAsync(UserRole.Admin);

            found.Should().BeEmpty();
        }
    }
}
