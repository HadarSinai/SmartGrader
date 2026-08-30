using FluentAssertions;
using MediatR;
using NSubstitute;
using SmartGrader.Application.UseCases.Assignments.BulkDeleteAssignments;
using SmartGrader.Application.UseCases.Assignments.DeleteAssignment;
using SmartGrader.Application.UseCases.Lessons.BulkDeleteLessons;
using SmartGrader.Application.UseCases.Lessons.DeleteLesson;
using SmartGrader.Application.UseCases.Students.BulkDeleteStudents;
using SmartGrader.Application.UseCases.Students.DeleteStudent;
using SmartGrader.Application.UseCases.Submissions.BulkDeleteSubmissions;
using SmartGrader.Application.UseCases.Submissions.DeleteSubmission;
using Xunit;

namespace SmartGrader.UnitTests.Handlers
{
    /// <summary>
    /// 🔴 מה שנבדק כאן הוא <b>שהמחיקה המרובה אינה מדיניות שנייה</b>: היא שולחת את פקודת
    /// המחיקה הבודדת עצמה, על כל מזהה, עם אותו היקף בעלות. שומר שנוסח מחדש בענף המרובה
    /// הוא העותק שמתיר בטעות את מה שהמקור חוסם — וכאן המחיר הוא עבודה של תלמידה.
    /// </summary>
    public class BulkDeleteHandlersTests
    {
        private const int TeacherId = 7;
        private const int LessonId = 3;
        private const int StudentId = 11;

        private readonly IMediator _mediator = Substitute.For<IMediator>();

        // ── שיעורים ──

        // פקודת מחיקה אחת לכל מזהה, ובכל אחת אותו TeacherId
        [Fact]
        public async Task BulkDeleteLessons_SendsTheSingleDeleteCommand_PerId()
        {
            var handler = new BulkDeleteLessonsHandler(_mediator);

            await handler.Handle(
                new BulkDeleteLessonsCommand(new[] { 1, 2 }, TeacherId), CancellationToken.None);

            await _mediator.Received(1).Send(
                new DeleteLessonCommand(1, TeacherId), Arg.Any<CancellationToken>());
            await _mediator.Received(1).Send(
                new DeleteLessonCommand(2, TeacherId), Arg.Any<CancellationToken>());
        }

        // ⚠️ מנהלת היא TeacherId = null, ואסור שההיקף יאבד בדרך — אחרת מחיקה מרובה
        // הייתה מסננת בעלות אחרת מהמחיקה הבודדת
        [Fact]
        public async Task BulkDeleteLessons_CarriesTheAdminScope()
        {
            var handler = new BulkDeleteLessonsHandler(_mediator);

            await handler.Handle(
                new BulkDeleteLessonsCommand(new[] { 1 }, null), CancellationToken.None);

            await _mediator.Received(1).Send(
                new DeleteLessonCommand(1, null), Arg.Any<CancellationToken>());
        }

        // ── תרגילים ──

        // מזהה השיעור נשלח עם כל תרגיל — הוא מה שקושר את התרגיל לשיעור שבבעלותה
        [Fact]
        public async Task BulkDeleteAssignments_SendsTheLessonIdWithEveryId()
        {
            var handler = new BulkDeleteAssignmentsHandler(_mediator);

            await handler.Handle(
                new BulkDeleteAssignmentsCommand(LessonId, new[] { 4, 5 }, TeacherId),
                CancellationToken.None);

            await _mediator.Received(1).Send(
                new DeleteAssignmentCommand(LessonId, 4, TeacherId), Arg.Any<CancellationToken>());
            await _mediator.Received(1).Send(
                new DeleteAssignmentCommand(LessonId, 5, TeacherId), Arg.Any<CancellationToken>());
        }

        // ── תלמידות ──

        // ⚠️ בלי TeacherId, בדיוק כמו המחיקה הבודדת: תלמידה היא משאב מוסדי משותף
        [Fact]
        public async Task BulkDeleteStudents_SendsTheSingleDeleteCommand_PerId()
        {
            var handler = new BulkDeleteStudentsHandler(_mediator);

            await handler.Handle(
                new BulkDeleteStudentsCommand(new[] { 8, 9 }), CancellationToken.None);

            await _mediator.Received(1).Send(
                new DeleteStudentCommand(8), Arg.Any<CancellationToken>());
            await _mediator.Received(1).Send(
                new DeleteStudentCommand(9), Arg.Any<CancellationToken>());
        }

        // ── הגשות ──

        // מזהה התלמידה נשלח עם כל הגשה — הוא מה שמונע מחיקת הגשה של תלמידה אחרת
        [Fact]
        public async Task BulkDeleteSubmissions_SendsTheStudentIdWithEveryId()
        {
            var handler = new BulkDeleteSubmissionsHandler(_mediator);

            await handler.Handle(
                new BulkDeleteSubmissionsCommand(StudentId, new[] { 21 }, TeacherId),
                CancellationToken.None);

            await _mediator.Received(1).Send(
                new DeleteSubmissionCommand(StudentId, 21, TeacherId), Arg.Any<CancellationToken>());
        }
    }
}
