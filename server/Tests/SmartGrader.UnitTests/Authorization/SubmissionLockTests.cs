using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Authorization
{
    /// <summary>
    /// נעילה סופית של הגשה. נפרד לגמרי מ-<c>Submission.CanResubmit</c>: זה שואל "האם
    /// המצב והציון מאפשרים ניסיון נוסף", וזה שואל "האם בכלל מותר לפתוח". הנעילה גוברת,
    /// ואפילו אישור מורה אינו עוקף אותה.
    /// </summary>
    public class SubmissionLockTests
    {
        private const int StudentId = 5;
        private const int AssignmentId = 11;
        private const int LessonId = 3;

        private static Submission SubmissionIn(SchoolClass? schoolClass, int? lessonId = LessonId)
        {
            var assignment = new TestAssignment(AssignmentId);
            if (lessonId.HasValue)
                assignment.LessonId = lessonId.Value;

            return new SubmissionBuilder(StudentId, AssignmentId)
                .WithNavigation(
                    student: new TestEntities.TestStudent(StudentId, classId: 20, schoolClass),
                    assignment: assignment)
                .Build();
        }

        private static ILessonResultRepository ResultsReturning(LessonResult? result)
        {
            var repo = Substitute.For<ILessonResultRepository>();
            repo.GetAsync(StudentId, LessonId, Arg.Any<CancellationToken>()).Returns(result);
            return repo;
        }

        private static LessonResult CompletedResult()
        {
            var result = LessonResult.Create(StudentId, LessonId);
            result.CompleteWith(80);
            return result;
        }

        // ── תנאי 1: הכיתה בארכיון ──

        // כיתה שהתגלגלה לשנה הבאה נועלת, בלי קשר לציון הסופי
        [Fact]
        public async Task IsLocked_IsTrue_WhenClassIsArchived()
        {
            var submission = SubmissionIn(TestEntities.Class(20, isArchived: true));

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(null), submission, CancellationToken.None);

            locked.Should().BeTrue();
        }

        // ── תנאי 2: הציון הסופי לתלמידה בשיעור ──

        // ⚠️ הנעילה היא לפי (תלמידה, שיעור) — שיעור מסתיים לכל תלמידה בנפרד
        [Fact]
        public async Task IsLocked_IsTrue_WhenLessonResultIsComplete()
        {
            var submission = SubmissionIn(TestEntities.Class(20));

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(CompletedResult()), submission, CancellationToken.None);

            locked.Should().BeTrue();
        }

        // ציון סופי שנפתח מחדש משחרר את הנעילה — זו רשת הביטחון לטעות של מורה
        [Fact]
        public async Task IsLocked_IsFalse_AfterLessonResultReopened()
        {
            var result = CompletedResult();
            result.Reopen();
            var submission = SubmissionIn(TestEntities.Class(20));

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(result), submission, CancellationToken.None);

            locked.Should().BeFalse();
        }

        // ── לא נעול ──

        // כיתה פעילה ובלי ציון סופי — פתוח
        [Fact]
        public async Task IsLocked_IsFalse_ForActiveClassWithNoResult()
        {
            var submission = SubmissionIn(TestEntities.Class(20));

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(null), submission, CancellationToken.None);

            locked.Should().BeFalse();
        }

        // בלי שיעור מזוהה אין מה לבדוק — לא ננעל, ולא קורס
        [Fact]
        public async Task IsLocked_IsFalse_WhenAssignmentHasNoLesson()
        {
            var submission = new SubmissionBuilder(StudentId, AssignmentId)
                .WithNavigation(student: new TestEntities.TestStudent(StudentId, classId: 20, TestEntities.Class(20)))
                .Build();

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(null), submission, CancellationToken.None);

            locked.Should().BeFalse();
        }

        // תכונות ניווט חסרות לא מפילות את הבדיקה
        [Fact]
        public async Task IsLocked_IsFalse_WhenNavigationPropertiesAreMissing()
        {
            var submission = new SubmissionBuilder(StudentId, AssignmentId).Build();

            var locked = await SubmissionLock.IsLockedAsync(
                ResultsReturning(null), submission, CancellationToken.None);

            locked.Should().BeFalse();
        }

        // ── ההודעה שהתלמידה רואה ──

        // אותו נוסח לשני מסלולי הנעילה — היא לא צריכה לדעת מי מהם תפס
        [Fact]
        public void Message_ExplainsBothLockReasons()
        {
            SubmissionLock.Message.Should().Contain("סוכם").And.Contain("ארכיון");
        }
    }
}
