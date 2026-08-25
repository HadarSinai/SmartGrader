using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.UseCases.Auth.Login;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// כניסה למערכת. 🔴 הבדיקה המרכזית כאן אינה "מה מוחזר" אלא <b>מה לא מוסגר</b>: שם
    /// משתמש שאינו קיים, סיסמה שגויה וחשבון נעול חייבים להיראות זהים לחלוטין מבחוץ.
    /// כל הבדל ביניהם — גם בנוסח ההודעה — הופך את הנקודה למונה חשבונות קיימים.
    /// </summary>
    public class LoginHandlerTests
    {
        private const string Username = "dana";
        private const string AnyPassword = "whatever";

        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
        private readonly IPasswordHasherService _hasher = Substitute.For<IPasswordHasherService>();
        private readonly IJwtTokenGenerator _tokens = Substitute.For<IJwtTokenGenerator>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private LoginHandler Handler() =>
            new(_users, _students, _hasher, _tokens, _unitOfWork);

        private static LoginCommand Command() =>
            new(new LoginRequestDto(Username, AnyPassword));

        private static User Teacher() =>
            User.Create(Username, "hash", "דנה כהן", UserRole.Teacher, "dana@school.org");

        private static User StudentUser() =>
            User.Create(Username, "hash", "נועה לוי", UserRole.Student);

        /// <summary>נועלת את החשבון בזמן אמת, כך שהנעילה עדיין בתוקף כשה-handler בודק אותה.</summary>
        private static User LockedTeacher()
        {
            var user = Teacher();
            var now = DateTime.UtcNow;

            user.RegisterFailedLogin(now);
            user.RegisterFailedLogin(now);
            user.RegisterFailedLogin(now);
            user.RegisterFailedLogin(now);
            user.RegisterFailedLogin(now);

            return user;
        }

        private void GivenUser(User? user) =>
            _users.GetByUsernameAsync(Username, Arg.Any<CancellationToken>()).Returns(user);

        private void GivenPasswordIsCorrect(bool correct) =>
            _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(correct);

        /// <summary>
        /// מריצה מסלול כישלון אחד על מערכת נקייה ומחזירה את ההודעה שיצאה — כדי שאפשר יהיה
        /// להשוות שלושה מסלולים זה לזה בתוך בדיקה אחת, בלי שיחלקו ביניהם תחליפים.
        /// </summary>
        private static async Task<string> FailureMessageAsync(User? user, bool passwordIsCorrect)
        {
            var users = Substitute.For<IUserRepository>();
            users.GetByUsernameAsync(Username, Arg.Any<CancellationToken>()).Returns(user);

            var hasher = Substitute.For<IPasswordHasherService>();
            hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(passwordIsCorrect);

            var handler = new LoginHandler(
                users,
                Substitute.For<IStudentRepository>(),
                hasher,
                Substitute.For<IJwtTokenGenerator>(),
                Substitute.For<IUnitOfWork>());

            var act = async () => await handler.Handle(Command(), CancellationToken.None);

            return (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Message;
        }

        // ── 🔴 מה שלא מוסגר ──

        // שלושת מסלולי הכישלון מחזירים בדיוק את אותה הודעה. שימי לב שבחשבון הנעול הסיסמה
        // דווקא *נכונה* — וגם אז אי אפשר להבחין בינו לבין שם משתמש שאינו קיים.
        [Fact]
        public async Task Handle_ReturnsOneIdenticalMessage_ForEveryFailurePath()
        {
            var unknownUser = await FailureMessageAsync(user: null, passwordIsCorrect: false);
            var wrongPassword = await FailureMessageAsync(Teacher(), passwordIsCorrect: false);
            var lockedAccount = await FailureMessageAsync(LockedTeacher(), passwordIsCorrect: true);

            wrongPassword.Should().Be(unknownUser);
            lockedAccount.Should().Be(unknownUser);
        }

        // שם משתמש שאינו קיים — נדחה, ובלי לגעת במסד
        [Fact]
        public async Task Handle_Throws_ForUnknownUsername()
        {
            GivenUser(null);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // ── ספירת הכישלונות ──

        // סיסמה שגויה נספרת ונשמרת. ⚠️ בלי השמירה הנעילה לא הייתה קורית לעולם:
        // GetByUsernameAsync מחזירה ישות מנותקת.
        [Fact]
        public async Task Handle_PersistsTheFailedAttempt_OnWrongPassword()
        {
            var user = Teacher();
            GivenUser(user);
            GivenPasswordIsCorrect(false);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            user.FailedLoginAttempts.Should().Be(1);
            await _users.Received().UpdateAsync(user, Arg.Any<CancellationToken>());
            await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // 🔴 ניסוח על חשבון שכבר נעול אינו מאריך את הנעילה. אחרת תוקפת שממשיכה לנחש
        // נועלת חשבון של מורה לצמיתות — מניעת שירות בלי לדעת שום סיסמה.
        [Fact]
        public async Task Handle_DoesNotExtendTheLockout_WhenAccountIsAlreadyLocked()
        {
            var user = LockedTeacher();
            var lockedUntil = user.LockoutEndsAt;
            GivenUser(user);
            GivenPasswordIsCorrect(false);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            user.LockoutEndsAt.Should().Be(lockedUntil);
            user.FailedLoginAttempts.Should().Be(User.MaxFailedLoginAttempts);
            await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        // כניסה מוצלחת מאפסת את המונה — טעויות הקלדה מפוזרות לא מצטברות לנעילה
        [Fact]
        public async Task Handle_ResetsTheFailureCounter_OnSuccessfulLogin()
        {
            var user = Teacher();
            user.RegisterFailedLogin(DateTime.UtcNow);
            user.RegisterFailedLogin(DateTime.UtcNow);
            GivenUser(user);
            GivenPasswordIsCorrect(true);

            await Handler().Handle(Command(), CancellationToken.None);

            user.FailedLoginAttempts.Should().Be(0);
            await _users.Received().UpdateAsync(user, Arg.Any<CancellationToken>());
        }

        // מונה נקי → אין כתיבה למסד בכל כניסה
        [Fact]
        public async Task Handle_DoesNotWrite_WhenTheCounterIsAlreadyClean()
        {
            GivenUser(Teacher());
            GivenPasswordIsCorrect(true);

            await Handler().Handle(Command(), CancellationToken.None);

            await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        // ── מה שחוזר בהצלחה ──

        // מורה מקבלת טוקן, שם ותפקיד — ובלי StudentId
        [Fact]
        public async Task Handle_ReturnsTokenAndProfile_ForTeacher()
        {
            var user = Teacher();
            GivenUser(user);
            GivenPasswordIsCorrect(true);
            _tokens.GenerateToken(user, null).Returns("jwt-for-dana");

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Token.Should().Be("jwt-for-dana");
            result.FullName.Should().Be("דנה כהן");
            result.Role.Should().Be(nameof(UserRole.Teacher));
            result.StudentId.Should().BeNull();
        }

        // ⚠️ תלמידה מקבלת StudentId מהמסד ולא מגוף הבקשה — הוא נכנס ל-claim של הטוקן,
        // וכל האזור האישי נשען עליו
        [Fact]
        public async Task Handle_IncludesStudentId_ForStudent()
        {
            var user = StudentUser();
            GivenUser(user);
            GivenPasswordIsCorrect(true);
            _students.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
                .Returns(new TestEntities.TestStudent(id: 42, classId: 20));

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.StudentId.Should().Be(42);
        }

        // חשבון תלמידה בלי שורת תלמידה נדחה בכניסה, עם הסבר — במקום מסכים ריקים
        [Fact]
        public async Task Handle_Throws_WhenStudentAccountHasNoStudentRow()
        {
            var user = StudentUser();
            GivenUser(user);
            GivenPasswordIsCorrect(true);
            _students.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((Student?)null);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ⚠️ ההודעה על חשבון יתום שונה בכוונה מהודעת הכניסה הגנרית — היא נאמרת *אחרי*
        // אימות מוצלח, ולכן אינה מסגירה דבר למי שאינה יודעת את הסיסמה
        [Fact]
        public async Task Handle_UsesADistinctMessage_ForOrphanStudentAccount()
        {
            var user = StudentUser();
            GivenUser(user);
            GivenPasswordIsCorrect(true);
            _students.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((Student?)null);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);
            var orphanMessage = (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Message;

            orphanMessage.Should().NotBe(await FailureMessageAsync(user: null, passwordIsCorrect: false));
        }
    }
}
