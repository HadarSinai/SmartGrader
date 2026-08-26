using FluentAssertions;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Repositories;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Integration
{
    /// <summary>
    /// חלון ההגשות שעליו מחושבים סיגנלי הכיתה. שלוש החלטות נבדקות כאן, וכל אחת מהן
    /// שקטה כשהיא נשברת: על מה חותכים את הזמן, מה נחשב "הוכרע", ומה נטען יחד עם ההגשה.
    /// </summary>
    public class ConcludedInRangeTests
    {
        private static readonly DateTime From = new(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime To = From.AddDays(1);

        private static SubmissionRepository Repository(SchoolDatabase db) => new(db.Context);

        // ── 🔴 חותכים על LastSubmittedAt, לא על GradedAt ──

        // הגשה שנכשלה בקומפילציה לא נבדקה מעולם, ולכן GradedAt שלה NULL. חיתוך לפיו היה
        // מפיל בשקט בדיוק את הסיגנלים של "התרגיל שבור" — אלה שהפעמון קיים בשבילם.
        [Fact]
        public async Task Includes_ASubmissionThatWasNeverGraded()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            var student = db.AddStudent(db.AddClass());
            var submission = db.Add(new SubmissionBuilder(student.Id, assignment.Id)
                .LastSubmittedAt(From.AddHours(9)).CompilationFailed().Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            submission.GradedAt.Should().BeNull();
            result.Should().ContainSingle();
        }

        // ── הטווח חצי־פתוח ──

        // הגשה בדיוק בתחילת החלון נכנסת
        [Fact]
        public async Task Includes_ASubmissionExactlyAtTheStart()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(From).Graded(80).Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            result.Should().ContainSingle();
        }

        // ⚠️ והגשה בדיוק בסופו — לא. בלי זה הגשה בחצות נספרת בשני ימים.
        [Fact]
        public async Task Excludes_ASubmissionExactlyAtTheEnd()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(To).Graded(80).Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            result.Should().BeEmpty();
        }

        // ── מה נחשב "הוכרע" ──

        // הגשה שעדיין בבדיקה אינה תוצאה של התלמידה, ולכן אינה נכנסת לא למונה ולא למכנה
        [Fact]
        public async Task Excludes_ASubmissionStillBeingGraded()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(From.AddHours(9)).Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            result.Should().BeEmpty();
        }

        // כישלון של המערכת שנבדקת אינו תוצאה של התלמידה — ונספר בכל זאת כשהוא סופי
        [Fact]
        public async Task Includes_ASubmissionThatFailedInTheAiStep()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(From.AddHours(9)).AiFailed().Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            result.Should().ContainSingle();
        }

        // ── 🔴 מה נטען יחד עם ההגשה ──

        // ClassSignalDetector מוותר על כל הגשה שאין לה Assignment ו-Lesson. בלי ה-Include
        // הניווט חוזר null, הגלאי מדלג על הכול, והפעמון פשוט מציג "אין התראות" — בלי שגיאה,
        // בלי לוג, ובלי שום סימן שמשהו נשבר.
        [Fact]
        public async Task Loads_TheAssignmentAndItsLesson()
        {
            using var db = new SchoolDatabase();
            var lesson = db.AddLesson(db.AddTeacher());
            var assignment = db.AddAssignment(lesson);
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(From.AddHours(9)).Graded(80).Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            var loaded = result.Should().ContainSingle().Subject;
            loaded.Assignment.Should().NotBeNull();
            loaded.Assignment!.Lesson.Should().NotBeNull();
            loaded.Assignment.Lesson!.Id.Should().Be(lesson.Id);
        }

        // ⚠️ התלמידה עצמה בכוונה אינה נטענת: הסיגנלים סופרים תלמידות ואינם נוקבים בשמן,
        // ומורה אינה צריכה רשימת נכשלות בפעמון.
        [Fact]
        public async Task DoesNotLoad_TheStudent()
        {
            using var db = new SchoolDatabase();
            var assignment = db.AddAssignment(db.AddLesson(db.AddTeacher()));
            db.Add(new SubmissionBuilder(db.AddStudent(db.AddClass()).Id, assignment.Id)
                .LastSubmittedAt(From.AddHours(9)).Graded(80).Build());

            var result = await Repository(db).GetConcludedInRangeAsync(From, To, teacherId: null);

            result.Should().ContainSingle().Which.Student.Should().BeNull();
        }
    }
}
