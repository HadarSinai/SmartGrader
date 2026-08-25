using FluentAssertions;
using NSubstitute;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Authorization
{
    /// <summary>
    /// מי רשאית להגיע לשיעור. ⚠️ "לא קיים" ו"לא שלך" מחזירים <b>אותה</b> חריגה בכוונה —
    /// 403 היה מאשר שהשיעור קיים, ומאפשר לגשש מזהים.
    /// </summary>
    public class LessonAccessTests
    {
        private const int OwnerTeacherId = 7;
        private const int OtherTeacherId = 8;
        private const int LessonId = 3;
        private const int ClassId = 20;

        private static ILessonRepository LessonsReturning(Lesson? lesson)
        {
            var repo = Substitute.For<ILessonRepository>();
            repo.GetByIdAsync(LessonId, Arg.Any<CancellationToken>()).Returns(lesson);
            return repo;
        }

        private static IStudentRepository StudentsReturning(Student? student)
        {
            var repo = Substitute.For<IStudentRepository>();
            repo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(student);
            return repo;
        }

        // ── בעלות ──

        // המורה שבבעלותה השיעור מקבלת אותו
        [Fact]
        public async Task GetOwnedOrThrow_ReturnsLesson_ForOwningTeacher()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId);

            var result = await LessonAccess.GetOwnedOrThrowAsync(
                LessonsReturning(lesson), LessonId, OwnerTeacherId, CancellationToken.None);

            result.Should().BeSameAs(lesson);
        }

        // ⚠️ מורה אחרת מקבלת NotFound — לא Forbidden. 403 היה מסגיר שהשיעור קיים.
        [Fact]
        public async Task GetOwnedOrThrow_ThrowsNotFound_ForNonOwningTeacher()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId);

            var act = async () => await LessonAccess.GetOwnedOrThrowAsync(
                LessonsReturning(lesson), LessonId, OtherTeacherId, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // שיעור שלא קיים — בדיוק אותה חריגה, כך ששני המקרים אינם ניתנים להבחנה
        [Fact]
        public async Task GetOwnedOrThrow_ThrowsNotFound_ForMissingLesson()
        {
            var act = async () => await LessonAccess.GetOwnedOrThrowAsync(
                LessonsReturning(null), LessonId, OwnerTeacherId, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // מנהלת עוברת בכל מקרה — teacherId null פירושו בלי סינון בעלות
        [Fact]
        public async Task GetOwnedOrThrow_AllowsAdmin_RegardlessOfOwner()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId);

            var result = await LessonAccess.GetOwnedOrThrowAsync(
                LessonsReturning(lesson), LessonId, teacherId: null, CancellationToken.None);

            result.Should().BeSameAs(lesson);
        }

        // ── גישת תלמידה: רק לשיעור של הכיתה שלה ──

        // תלמידה בכיתה שהשיעור משויך אליה — עוברת
        [Fact]
        public async Task GetAccessibleOrThrow_AllowsStudentInAssignedClass()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId, TestEntities.Class(ClassId));
            var student = new TestEntities.TestStudent(id: 5, classId: ClassId);

            var result = await LessonAccess.GetAccessibleOrThrowAsync(
                LessonsReturning(lesson), StudentsReturning(student),
                LessonId, teacherId: null, studentId: 5, CancellationToken.None);

            result.Should().BeSameAs(lesson);
        }

        // 🔴 תלמידה מכיתה אחרת לא מגיעה לשיעור — גם אם היא מחוברת למערכת
        [Fact]
        public async Task GetAccessibleOrThrow_ThrowsNotFound_ForStudentInAnotherClass()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId, TestEntities.Class(ClassId));
            var student = new TestEntities.TestStudent(id: 5, classId: 999);

            var act = async () => await LessonAccess.GetAccessibleOrThrowAsync(
                LessonsReturning(lesson), StudentsReturning(student),
                LessonId, teacherId: null, studentId: 5, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // שיעור בלי כיתות משויכות אינו נגיש לאף תלמידה
        [Fact]
        public async Task GetAccessibleOrThrow_ThrowsNotFound_WhenLessonHasNoClasses()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId);
            var student = new TestEntities.TestStudent(id: 5, classId: ClassId);

            var act = async () => await LessonAccess.GetAccessibleOrThrowAsync(
                LessonsReturning(lesson), StudentsReturning(student),
                LessonId, teacherId: null, studentId: 5, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // תלמידה שלא נמצאה במסד — נדחית, לא עוברת בשקט
        [Fact]
        public async Task GetAccessibleOrThrow_ThrowsNotFound_WhenStudentMissing()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId, TestEntities.Class(ClassId));

            var act = async () => await LessonAccess.GetAccessibleOrThrowAsync(
                LessonsReturning(lesson), StudentsReturning(null),
                LessonId, teacherId: null, studentId: 5, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // מנהלת: שני המזהים null — רואה הכול, בלי בדיקת כיתה
        [Fact]
        public async Task GetAccessibleOrThrow_AllowsAdmin_WithoutClassCheck()
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId);

            var result = await LessonAccess.GetAccessibleOrThrowAsync(
                LessonsReturning(lesson), StudentsReturning(null),
                LessonId, teacherId: null, studentId: null, CancellationToken.None);

            result.Should().BeSameAs(lesson);
        }

        // ── הבדיקה הבסיסית של שיוך כיתה ──

        [Theory]
        [InlineData(ClassId, true)]
        [InlineData(999, false)]
        public void IsAssignedToClass_MatchesOnClassId(int classId, bool expected)
        {
            var lesson = new TestEntities.TestLesson(LessonId, OwnerTeacherId, TestEntities.Class(ClassId));

            LessonAccess.IsAssignedToClass(lesson, classId).Should().Be(expected);
        }
    }
}
