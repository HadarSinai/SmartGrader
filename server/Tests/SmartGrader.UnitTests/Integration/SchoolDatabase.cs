using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Data;

namespace SmartGrader.UnitTests.Integration
{
    /// <summary>
    /// בית ספר קטן במסד SQLite אמיתי בזיכרון, עם הסכמה האמיתית ועם EF Core האמיתי.
    /// <para>
    /// ⚠️ זו הרמה היחידה שתופסת <c>Include</c> חסר, סינון בעלות שגוי, או אינדקס ייחודי
    /// שאינו נאכף — כל אלה נראים תקינים לגמרי מול תחליף בזיכרון.
    /// </para>
    /// <para>
    /// ⚠️ החיבור נשאר <b>פתוח</b> לכל אורך חיי המחלקה: ב-SQLite מצב <c>:memory:</c> המסד חי
    /// בחיבור עצמו, וסגירתו מוחקת אותו על המקום.
    /// </para>
    /// </summary>
    public sealed class SchoolDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private int _nextName;

        public GradeSheetContext Context { get; }

        public SchoolDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            Context = new GradeSheetContext(
                new DbContextOptionsBuilder<GradeSheetContext>()
                    .UseSqlite(_connection)
                    .Options);

            Context.Database.EnsureCreated();
        }

        /// <summary>
        /// יוצרת ישות דרך הבנאי המוגן שלה — בדיוק כמו ש-EF עצמו יוצר אותה בקריאה מהמסד.
        /// <para>
        /// ⚠️ ולא תת-מחלקה כמו <c>TestAssignment</c>: תת-מחלקה היא טיפוס CLR שאינו במודל,
        /// ו-EF אינו יודע לאיזו טבלה לשייך אותה בהוספה. כאן נוצר הטיפוס עצמו.
        /// </para>
        /// </summary>
        private static T New<T>() where T : class =>
            (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

        private string UniqueName(string prefix) => $"{prefix}-{++_nextName}";

        public User AddTeacher()
        {
            var name = UniqueName("teacher");
            var teacher = User.Create(name, "hash", "דנה כהן", UserRole.Teacher, $"{name}@school.org");

            Context.Users.Add(teacher);
            Context.SaveChanges();
            return teacher;
        }

        public SchoolClass AddClass(bool isArchived = false)
        {
            var schoolClass = SchoolClass.Create(UniqueName("כיתה"), academicYear: 5786);
            schoolClass.IsArchived = isArchived;

            Context.SchoolClasses.Add(schoolClass);
            Context.SaveChanges();
            return schoolClass;
        }

        public Lesson AddLesson(User teacher, params SchoolClass[] classes)
        {
            var course = Course.Create(UniqueName("קורס"), teacher.Id);
            Context.Courses.Add(course);
            Context.SaveChanges();

            var lesson = New<Lesson>();
            lesson.Subject = UniqueName("שיעור");
            lesson.TeacherId = teacher.Id;
            lesson.CourseId = course.Id;
            lesson.LessonDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var schoolClass in classes)
                lesson.Classes.Add(Context.SchoolClasses.Find(schoolClass.Id)!);

            Context.Lessons.Add(lesson);
            Context.SaveChanges();
            return lesson;
        }

        public Assignment AddAssignment(Lesson lesson)
        {
            var assignment = New<Assignment>();
            assignment.Title = UniqueName("תרגיל");
            assignment.LessonId = lesson.Id;

            Context.Assignments.Add(assignment);
            Context.SaveChanges();
            return assignment;
        }

        public Student AddStudent(SchoolClass schoolClass)
        {
            var student = New<Student>();
            student.FullName = UniqueName("תלמידה");
            student.ClassId = schoolClass.Id;

            Context.Students.Add(student);
            Context.SaveChanges();
            return student;
        }

        /// <summary>מוסיפה הגשה כפי שהיא נבנתה, ומנתקת אותה מהמעקב כדי שקריאה תחזור מהמסד.</summary>
        public Submission Add(Submission submission)
        {
            Context.Submissions.Add(submission);
            Context.SaveChanges();
            Context.ChangeTracker.Clear();
            return submission;
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
