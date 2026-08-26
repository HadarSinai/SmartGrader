using FluentAssertions;
using SmartGrader.Infrastructure.Repositories;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Integration
{
    /// <summary>
    /// 🔴 מי רואה מה. סינון הבעלות הוא ה-<c>Where</c> היחיד שמפריד בין המורות, וחלק ממנו
    /// עובר <b>שתי קפיצות ניווט</b> (הגשה ← תרגיל ← שיעור ← מורה) — בדיוק החלק שתחליף
    /// בזיכרון אינו יכול לאמת, כי אצלו ההצמדה תמיד מושלמת.
    /// <para>
    /// ⚠️ <c>teacherId == null</c> פירושו מנהל/ת, כלומר בלי סינון. כל בדיקה כאן בודקת גם
    /// את זה, כי "אין סינון" קל מדי לממש בטעות כ"אין הגבלה" גם למורה.
    /// </para>
    /// </summary>
    public class OwnershipFilterTests
    {
        // מורה מקבלת רק את השיעורים שלה
        [Fact]
        public async Task Lessons_AreScopedToTheOwningTeacher()
        {
            using var db = new SchoolDatabase();
            var mine = db.AddTeacher();
            db.AddLesson(mine);
            db.AddLesson(db.AddTeacher());

            var result = await new LessonRepository(db.Context).GetAllAsync(null, mine.Id);

            result.Should().ContainSingle().Which.TeacherId.Should().Be(mine.Id);
        }

        // מנהלת (null) רואה את כל השיעורים
        [Fact]
        public async Task Lessons_AreNotScoped_ForAdmin()
        {
            using var db = new SchoolDatabase();
            db.AddLesson(db.AddTeacher());
            db.AddLesson(db.AddTeacher());

            var result = await new LessonRepository(db.Context).GetAllAsync(null, null);

            result.Should().HaveCount(2);
        }

        // 🔴 הגשה מסוננת דרך התרגיל והשיעור — שתי קפיצות ניווט עד לבעלות
        [Fact]
        public async Task Submissions_AreScopedThroughAssignmentAndLesson()
        {
            using var db = new SchoolDatabase();
            var mine = db.AddTeacher();
            var window = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

            var myAssignment = db.AddAssignment(db.AddLesson(mine));
            var otherAssignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            var schoolClass = db.AddClass();
            db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, myAssignment.Id)
                .LastSubmittedAt(window).Graded(80).Build());
            db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, otherAssignment.Id)
                .LastSubmittedAt(window).Graded(80).Build());

            var result = await new SubmissionRepository(db.Context).GetConcludedInRangeAsync(
                window.AddDays(-1), window.AddDays(1), mine.Id);

            result.Should().ContainSingle().Which.AssignmentId.Should().Be(myAssignment.Id);
        }

        [Fact]
        public async Task Submissions_AreNotScoped_ForAdmin()
        {
            using var db = new SchoolDatabase();
            var window = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
            var myAssignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            var otherAssignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            var schoolClass = db.AddClass();
            db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, myAssignment.Id)
                .LastSubmittedAt(window).Graded(80).Build());
            db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, otherAssignment.Id)
                .LastSubmittedAt(window).Graded(80).Build());

            var result = await new SubmissionRepository(db.Context).GetConcludedInRangeAsync(
                window.AddDays(-1), window.AddDays(1), teacherId: null);

            result.Should().HaveCount(2);
        }

        // ── תלמידות: הבעלות נגזרת דרך הכיתות ──

        // רק תלמידות הכיתות שנמסרו חוזרות
        [Fact]
        public async Task Students_AreScopedToTheGivenClasses()
        {
            using var db = new SchoolDatabase();
            var mine = db.AddClass();
            db.AddStudent(mine);
            db.AddStudent(db.AddClass());

            var result = await new StudentRepository(db.Context)
                .GetByClassIdsAsync(new[] { mine.Id }, includeArchived: true, includeCounts: false);

            result.Should().ContainSingle().Which.ClassId.Should().Be(mine.Id);
        }

        // ⚠️ רשימת כיתות ריקה מחזירה ריק — לא את כל בית הספר. זה הכשל המסוכן ביותר,
        // כי "אין סינון" נראה כמו "אין הגבלה".
        [Fact]
        public async Task Students_AreEmpty_ForNoClasses()
        {
            using var db = new SchoolDatabase();
            db.AddStudent(db.AddClass());

            var result = await new StudentRepository(db.Context)
                .GetByClassIdsAsync(Array.Empty<int>(), includeArchived: true, includeCounts: false);

            result.Should().BeEmpty();
        }

        // תלמידה בכיתה בארכיון מסוננת כשלא ביקשו ארכיון
        [Fact]
        public async Task Students_ExcludeArchivedClasses_WhenNotRequested()
        {
            using var db = new SchoolDatabase();
            var archived = db.AddClass(isArchived: true);
            db.AddStudent(archived);

            var result = await new StudentRepository(db.Context)
                .GetByClassIdsAsync(new[] { archived.Id }, includeArchived: false, includeCounts: false);

            result.Should().BeEmpty();
        }

        // ── קורסים ──

        [Fact]
        public async Task Courses_AreScopedToTheOwningTeacher()
        {
            using var db = new SchoolDatabase();
            var mine = db.AddTeacher();
            db.AddLesson(mine);
            db.AddLesson(db.AddTeacher());

            var result = await new CourseRepository(db.Context).GetAllAsync(mine.Id);

            result.Should().ContainSingle().Which.TeacherId.Should().Be(mine.Id);
        }
    }
}
