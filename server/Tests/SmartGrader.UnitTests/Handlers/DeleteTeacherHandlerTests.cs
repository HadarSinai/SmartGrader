using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.UseCases.Teachers.DeleteTeacher;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// מחיקת חשבון מורה. ⚠️ בלי השומר שסופר שיעורים וקורסים, המחיקה נופלת ברמת המסד
    /// (‏<c>Restrict</c> על <c>Lesson.TeacherId</c> ו-<c>Course.TeacherId</c>) כשגיאה 500
    /// סתומה — במקום הודעה שמסבירה מה חוסם ובכמה.
    /// </summary>
    public class DeleteTeacherHandlerTests
    {
        private const int TeacherId = 7;
        private const int AdminId = 1;
        private const int SomeoneElseId = 99;

        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly ILessonRepository _lessons = Substitute.For<ILessonRepository>();
        private readonly ICourseRepository _courses = Substitute.For<ICourseRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private DeleteTeacherHandler Handler() =>
            new(_users, _lessons, _courses, _unitOfWork);

        // ⚠️ דרך TestEntities ולא דרך User.Create: הבנאי אינו קובע Id, וכל בדיקה שמשווה
        // מזהים הייתה משווה 0 למספר אמיתי ולעולם לא מזהה את המקרה שהיא נכתבה בשבילו.
        private static User Teacher() =>
            TestEntities.UserWithId(TeacherId, UserRole.Teacher);

        private static User Admin() =>
            TestEntities.UserWithId(AdminId, UserRole.Admin, "מנהלת", "admin@school.org");

        private void GivenUser(User? user) =>
            _users.GetByIdAsync(TeacherId, Arg.Any<CancellationToken>()).Returns(user);

        private void GivenWorkload(int lessons, int courses)
        {
            _lessons.CountByTeacherIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(lessons);
            _courses.CountByTeacherIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(courses);
        }

        private async Task<string> RefusalMessageAsync(DeleteTeacherCommand command)
        {
            var act = async () => await Handler().Handle(command, CancellationToken.None);

            return (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Message;
        }

        // חשבון שאינו קיים
        [Fact]
        public async Task Handle_ThrowsNotFound_ForAnUnknownUser()
        {
            GivenUser(null);

            var act = async () => await Handler().Handle(
                new DeleteTeacherCommand(TeacherId, AdminId), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── סדר הבדיקות ──

        // ⚠️ הבדיקה העצמית קודמת לבדיקת התפקיד, וזה לא סגנון: המסך כולו הוא Admin-only,
        // ולכן המוחקת היא תמיד מנהלת. אחרי בדיקת התפקיד, מנהלת שמוחקת את עצמה הייתה
        // מקבלת "ניתן למחוק מכאן חשבונות מורות בלבד" — נכון טכנית, ולא עונה על מה שקרה.
        [Fact]
        public async Task Handle_RefusesSelfDeletion_BeforeCheckingTheRole()
        {
            GivenUser(Admin());
            GivenWorkload(0, 0);

            var selfMessage = await RefusalMessageAsync(new DeleteTeacherCommand(TeacherId, AdminId));
            var roleMessage = await RefusalMessageAsync(new DeleteTeacherCommand(TeacherId, SomeoneElseId));

            selfMessage.Should().NotBe(roleMessage);
        }

        // המסך מוחק מורות בלבד — תלמידה נמחקת במסלול אחר, שמוחק גם את העבודה שלה
        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.Student)]
        public async Task Handle_RefusesToDeleteAnyoneWhoIsNotATeacher(UserRole role)
        {
            GivenUser(TestEntities.UserWithId(TeacherId, role, "מישהי"));
            GivenWorkload(0, 0);

            var act = async () => await Handler().Handle(
                new DeleteTeacherCommand(TeacherId, AdminId), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            await _users.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        // ── העבודה שתישאר בלי בעלים ──

        // מורה עם שיעורים אינה נמחקת, ושום דבר לא נשמר
        [Fact]
        public async Task Handle_RefusesWhenTheTeacherStillOwnsLessons()
        {
            GivenUser(Teacher());
            GivenWorkload(lessons: 3, courses: 0);

            var act = async () => await Handler().Handle(
                new DeleteTeacherCommand(TeacherId, AdminId), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
            await _users.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // וגם קורסים לבדם חוסמים
        [Fact]
        public async Task Handle_RefusesWhenTheTeacherStillOwnsCourses()
        {
            GivenUser(Teacher());
            GivenWorkload(lessons: 0, courses: 2);

            var act = async () => await Handler().Handle(
                new DeleteTeacherCommand(TeacherId, AdminId), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ⚠️ ההודעה נושאת את המספרים עצמם — "יש לה 3 שיעורים ו-2 קורסים". בלעדיהם המנהלת
        // יודעת שנחסמה ולא יודעת מה להעביר.
        [Fact]
        public async Task Handle_NamesBothCounts_InTheRefusal()
        {
            GivenUser(Teacher());
            GivenWorkload(lessons: 3, courses: 2);

            var message = await RefusalMessageAsync(new DeleteTeacherCommand(TeacherId, AdminId));

            message.Should().Contain("3").And.Contain("2").And.Contain("דנה כהן");
        }

        // ── המסלול התקין ──

        // מורה בלי שיעורים ובלי קורסים נמחקת ונשמרת
        [Fact]
        public async Task Handle_DeletesATeacherWithNoWorkLeftBehind()
        {
            var teacher = Teacher();
            GivenUser(teacher);
            GivenWorkload(0, 0);

            await Handler().Handle(new DeleteTeacherCommand(TeacherId, AdminId), CancellationToken.None);

            await _users.Received().DeleteAsync(teacher, Arg.Any<CancellationToken>());
            await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
