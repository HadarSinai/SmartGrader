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
    /// אילו תלמידות מורה רואה. 🔴 הפער שזה סגר: דוחות וייצוא קראו ל-<c>GetAllAsync</c>,
    /// כלומר <b>כל מורה ייצאה רשימה ובה כל תלמידה בבית הספר</b>.
    /// </summary>
    public class StudentScopeTests
    {
        private const int TeacherId = 7;

        private static IStudentRepository Students() => Substitute.For<IStudentRepository>();
        private static ILessonRepository Lessons() => Substitute.For<ILessonRepository>();

        private static IReadOnlyList<Student> Roster(params int[] ids) =>
            ids.Select(id => (Student)new TestEntities.TestStudent(id, classId: 20)).ToList();

        // ── מנהלת: בלי סינון ──

        // teacherId null = מנהלת, ומקבלת את כל בית הספר
        [Fact]
        public async Task GetVisible_ReturnsEveryone_ForAdmin()
        {
            var students = Students();
            students.GetAllAsync(false, Arg.Any<CancellationToken>()).Returns(Roster(1, 2, 3));

            var result = await StudentScope.GetVisibleAsync(
                students, Lessons(), teacherId: null,
                includeArchived: false, includeCounts: false, CancellationToken.None);

            result.Should().HaveCount(3);
        }

        // ── מורה: רק דרך הכיתות של השיעורים שלה ──

        // 🔴 הבעלות נגזרת בעקיפין — לתלמידה אין TeacherId. מורה מקבלת רק את מי שבכיתות
        // המשויכות לשיעורים שבבעלותה, ולעולם לא את כל בית הספר.
        [Fact]
        public async Task GetVisible_ScopesToClassesOfOwnLessons_ForTeacher()
        {
            var lessons = Lessons();
            lessons.GetAllAsync(null, TeacherId, Arg.Any<CancellationToken>()).Returns(new List<Lesson>
            {
                new TestEntities.TestLesson(1, TeacherId, TestEntities.Class(20)),
                new TestEntities.TestLesson(2, TeacherId, TestEntities.Class(21))
            });

            var students = Students();
            students.GetByClassIdsAsync(
                    Arg.Is<IReadOnlyList<int>>(ids => ids.Contains(20) && ids.Contains(21) && ids.Count == 2),
                    false, false, Arg.Any<CancellationToken>())
                .Returns(Roster(1, 2));

            var result = await StudentScope.GetVisibleAsync(
                students, lessons, TeacherId,
                includeArchived: false, includeCounts: false, CancellationToken.None);

            result.Should().HaveCount(2);
            await students.DidNotReceive().GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        // אותה כיתה בשני שיעורים נספרת פעם אחת
        [Fact]
        public async Task GetVisible_DeduplicatesClassIds()
        {
            var lessons = Lessons();
            lessons.GetAllAsync(null, TeacherId, Arg.Any<CancellationToken>()).Returns(new List<Lesson>
            {
                new TestEntities.TestLesson(1, TeacherId, TestEntities.Class(20)),
                new TestEntities.TestLesson(2, TeacherId, TestEntities.Class(20))
            });

            var students = Students();
            students.GetByClassIdsAsync(
                    Arg.Is<IReadOnlyList<int>>(ids => ids.Count == 1 && ids[0] == 20),
                    Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Roster(1));

            var result = await StudentScope.GetVisibleAsync(
                students, lessons, TeacherId,
                includeArchived: false, includeCounts: false, CancellationToken.None);

            result.Should().HaveCount(1);
        }

        // ⚠️ מורה בלי שיעורים מקבלת רשימה ריקה — לא את כל בית הספר. זה הכשל
        // המסוכן ביותר: "אין סינון" קל מדי לממש בטעות כ"אין הגבלה".
        [Fact]
        public async Task GetVisible_ReturnsEmpty_ForTeacherWithNoLessons()
        {
            var lessons = Lessons();
            lessons.GetAllAsync(null, TeacherId, Arg.Any<CancellationToken>()).Returns(new List<Lesson>());

            var students = Students();
            students.GetByClassIdsAsync(
                    Arg.Any<IReadOnlyList<int>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new List<Student>());

            var result = await StudentScope.GetVisibleAsync(
                students, lessons, TeacherId,
                includeArchived: false, includeCounts: false, CancellationToken.None);

            result.Should().BeEmpty();
            await students.DidNotReceive().GetAllAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        // הדגלים מועברים כמו שהם — ייצוא ורשימה משתמשים באותה הגדרת בעלות עם אפשרויות שונות
        [Fact]
        public async Task GetVisible_PassesFlagsThrough()
        {
            var lessons = Lessons();
            lessons.GetAllAsync(null, TeacherId, Arg.Any<CancellationToken>()).Returns(new List<Lesson>
            {
                new TestEntities.TestLesson(1, TeacherId, TestEntities.Class(20))
            });

            var students = Students();
            students.GetByClassIdsAsync(
                    Arg.Any<IReadOnlyList<int>>(), true, true, Arg.Any<CancellationToken>())
                .Returns(Roster(1));

            var result = await StudentScope.GetVisibleAsync(
                students, lessons, TeacherId,
                includeArchived: true, includeCounts: true, CancellationToken.None);

            result.Should().HaveCount(1);
        }
    }
}
