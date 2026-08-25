using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Application.UseCases.Auth.ForgotPassword;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// בקשת קישור לאיפוס סיסמה. 🔴 העיקרון היחיד שמנהל את כל ה-handler: <b>התשובה זהה
    /// בכל מסלול</b> — כתובת רשומה, כתובת שאינה קיימת, חשבון תלמידה ותקלת SMTP כאחד.
    /// כל הבדל שדולף החוצה, ובכלל זה חריגה שהופכת ל-500, הופך את הנקודה למונה חשבונות
    /// רשומים: מי שמריצה עליה רשימת כתובות מקבלת בחזרה מי מהן במערכת.
    /// </summary>
    public class ForgotPasswordHandlerTests
    {
        private const string Email = "dana@school.org";
        private const string BaseUrl = "https://smartgrader.example";

        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly IPasswordResetTokenRepository _tokens = Substitute.For<IPasswordResetTokenRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IEmailSender _email = Substitute.For<IEmailSender>();
        private readonly IClientUrlProvider _clientUrl = Substitute.For<IClientUrlProvider>();
        private readonly ILogWriter _log = Substitute.For<ILogWriter>();

        public ForgotPasswordHandlerTests()
        {
            _clientUrl.IsConfigured.Returns(true);
            _clientUrl.BaseUrl.Returns(BaseUrl);
            _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);
        }

        private ForgotPasswordHandler Handler() =>
            new(_users, _tokens, _unitOfWork, _email, _clientUrl, _log);

        private static ForgotPasswordCommand Command() =>
            new(new ForgotPasswordRequestDto(Email));

        private static User Teacher() =>
            User.Create("dana", "hash", "דנה כהן", UserRole.Teacher, Email);

        private static User StudentUser() =>
            User.Create("noa", "hash", "נועה לוי", UserRole.Student, Email);

        private void GivenUser(User? user) =>
            _users.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(user);

        private Task NoEmailWasSent() =>
            _email.DidNotReceive().SendAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        private Task AnErrorWasLogged() =>
            _log.Received().WriteAsync(
                LogActionTypes.PasswordResetEmailFailed,
                Arg.Any<string>(),
                LogStatuses.Error,
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<CancellationToken>());

        // ── 🔴 כל המסלולים נראים אותו דבר מבחוץ ──

        // כתובת שאינה במערכת — יוצא בשקט, בלי חריגה ובלי מייל
        [Fact]
        public async Task Handle_DoesNothingAndDoesNotThrow_ForUnknownEmail()
        {
            GivenUser(null);

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await NoEmailWasSent();
            await _tokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        }

        // ⚠️ תלמידה אינה משחזרת סיסמה במייל — המורה מאפסת לה. שורת תלמידה שיש בה כתובת
        // בכל זאת לא מקבלת מסלול שעוקף את המורה.
        [Fact]
        public async Task Handle_SendsNothing_ForStudentAccount()
        {
            GivenUser(StudentUser());

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await NoEmailWasSent();
        }

        // 🔴 תקלת SMTP לא יוצאת החוצה כחריגה. 500 על כתובת רשומה מול 200 על כתובת שאינה
        // קיימת הוא בדיוק ההבדל שכל ה-handler נכתב כדי למחוק.
        [Fact]
        public async Task Handle_DoesNotThrow_WhenSendingBlowsUp()
        {
            GivenUser(Teacher());
            _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<bool>(_ => throw new InvalidOperationException("SMTP down"));

            var act = async () => await Handler().Handle(Command(), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await AnErrorWasLogged();
        }

        // ⚠️ SMTP שאינו מוגדר מחזיר false בשקט — בלי שורת הלוג, מערכת שבורה נראית כמו יום שקט
        [Fact]
        public async Task Handle_LogsAnError_WhenSmtpIsNotConfigured()
        {
            GivenUser(Teacher());
            _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            await Handler().Handle(Command(), CancellationToken.None);

            await AnErrorWasLogged();
        }

        // כתובת לקוח שאינה מוגדרת — לא נשלח מייל עם קישור שבור, ונרשמת תקלה
        [Fact]
        public async Task Handle_SendsNothingAndLogs_WhenClientUrlIsNotConfigured()
        {
            GivenUser(Teacher());
            _clientUrl.IsConfigured.Returns(false);

            await Handler().Handle(Command(), CancellationToken.None);

            await NoEmailWasSent();
            await AnErrorWasLogged();
        }

        // ── המסלול התקין ──

        // מורה רשומה מקבלת מייל לכתובת שלה, ובו קישור לכתובת הלקוח המוגדרת
        [Fact]
        public async Task Handle_SendsTheResetLink_ToTheRegisteredAddress()
        {
            GivenUser(Teacher());

            await Handler().Handle(Command(), CancellationToken.None);

            await _email.Received().SendAsync(
                Email,
                Arg.Any<string>(),
                Arg.Is<string>(body => body.Contains($"{BaseUrl}/reset-password?token=")),
                Arg.Any<CancellationToken>());
        }

        // 🔴 מה שנשמר אינו מה שנשלח: בטבלה יושב הגיבוב בלבד, ומי שקוראת את המסד אינה
        // יכולה לבנות ממנו את הקישור
        [Fact]
        public async Task Handle_StoresTheHash_NeverTheTokenThatWasMailed()
        {
            GivenUser(Teacher());
            PasswordResetToken? stored = null;
            _ = _tokens.AddAsync(
                Arg.Do<PasswordResetToken>(t => stored = t), Arg.Any<CancellationToken>());
            string? body = null;
            _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<string>(b => body = b),
                Arg.Any<CancellationToken>()).Returns(true);

            await Handler().Handle(Command(), CancellationToken.None);

            stored!.TokenHash.Should().NotBeNullOrWhiteSpace();
            body.Should().NotContain(stored.TokenHash);
        }

        // קישור חדש גובר על כל קישור פתוח — קישור שהגיע לתיבה הלא נכונה מפסיק לעבוד
        [Fact]
        public async Task Handle_InvalidatesOutstandingLinks_BeforeIssuingANewOne()
        {
            GivenUser(Teacher());

            await Handler().Handle(Command(), CancellationToken.None);

            await _tokens.Received().InvalidateAllForUserAsync(
                Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        }

        // ⚠️ שמירה לפני שליחה. בסדר ההפוך, כשל בשמירה משאיר במייל קישור שאין לו שורה —
        // מורה שלוחצת ומקבלת "הקישור אינו תקף" בלי סיבה נראית לעין.
        [Fact]
        public async Task Handle_SavesTheToken_BeforeSendingTheEmail()
        {
            GivenUser(Teacher());

            await Handler().Handle(Command(), CancellationToken.None);

            Received.InOrder(() =>
            {
                _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
                _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<CancellationToken>());
            });
        }
    }
}
