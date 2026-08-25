
using SmartGrader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartGrader.Infrastructure.Data
{
    public class GradeSheetContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionAttempt> SubmissionAttempts { get; set; }
        public DbSet<LessonResult> LessonResults { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public GradeSheetContext(DbContextOptions<GradeSheetContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.Submissions)
                .WithOne(s => s.Student)
                .HasForeignKey(s => s.StudentId);

            // ⚠️ Restrict, לא Cascade: השרשרת Lesson → Assignment → Submission הייתה על ברירת המחדל
            // של EF, כך שמחיקת שיעור אחת מוחקת בשקט את כל התרגילים, כל ההגשות (קוד, משוב, ציונים)
            // וכל הציונים הסופיים — ועוקפת לגמרי את ההגנה שב-DeleteSubmissionHandler, שמסרבת למחוק
            // הגשה שנבדקה. המחיקה המדורגת נחסמת ברמת ה-DB; המחיקה עצמה נעשית מפורשות ב-handlers,
            // אחרי בדיקה שאין עבודת תלמידות מתחת.
            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Assignments)
                .WithOne(a => a.Lesson)
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasMany(a => a.Submissions)
                .WithOne(s => s.Assignment)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // אותו נימוק: בלי זה מחיקת שיעור בלי תרגילים עדיין מוחקת את הציונים הסופיים שלו
            modelBuilder.Entity<LessonResult>()
                .HasOne(r => r.Lesson)
                .WithMany()
                .HasForeignKey(r => r.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // הגשה אחת בדיוק לכל (תלמידה, תרגיל). הבדיקה ב-CreateSubmissionHandler לבדה אינה
            // מספיקה: שתי לחיצות במקביל עוברות אותה שתיהן ויוצרות שתי שורות מנוקדות, שנספרות
            // שתיהן בממוצע. האכיפה האמיתית היא כאן.
            modelBuilder.Entity<Submission>()
                .HasIndex(s => new { s.StudentId, s.AssignmentId })
                .IsUnique();

            // היסטוריית הניסיונות. Cascade כאן ולא Restrict כמו בשאר שרשרת העבודה הבדוקה:
            // ניסיון אינו רשומה עצמאית אלא תמונת מצב של ההגשה, ואין לו שום משמעות בלעדיה.
            // מחיקת הגשה שנבדקה נחסמת ממילא ב-DeleteSubmissionHandler.
            modelBuilder.Entity<SubmissionAttempt>(attempt =>
            {
                attempt.HasOne(a => a.Submission)
                    .WithMany(s => s.Attempts)
                    .HasForeignKey(a => a.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                attempt.HasIndex(a => new { a.SubmissionId, a.AttemptNumber }).IsUnique();
            });

            // ברירות מחדל ברמת ה-DB, כדי ששורות שנוצרו לפני העמודות האלה לא ייקלטו כאפס:
            // RetryThreshold=0 היה סוגר כל הגשה לנצח, ו-TestsAllocation=0 היה נראה כמו תרגיל
            // מחלקות ונותן 100 לכל תלמידה דרך שער "הכול שערים" ב-ScoreCalculator.
            //
            // ⚠️ HasSentinel(-1) אינו קישוט: בלעדיו EF משמיט את העמודה מה-INSERT כשהערך שווה
            // לברירת המחדל של CLR, ולכן TestsAllocation=0 — ערך חוקי לגמרי לתרגיל מחלקות —
            // היה נכתב ל-DB כ-100.
            modelBuilder.Entity<Assignment>()
                .Property(a => a.TestsAllocation)
                .HasDefaultValue(Assignment.TotalPoints)
                .HasSentinel(-1);

            modelBuilder.Entity<Assignment>()
                .Property(a => a.RetryThreshold)
                .HasDefaultValue(Assignment.DefaultRetryThreshold)
                .HasSentinel(-1);

            modelBuilder.Entity<Submission>()
                .Property(s => s.AttemptNumber)
                .HasDefaultValue(1)
                .HasSentinel(-1);

            modelBuilder.Entity<Assignment>()
                .Property(a => a.GradingMode)
                .HasConversion<string>();

            modelBuilder.Entity<Student>()
                .HasMany(s => s.LessonResults)
                .WithOne(r => r.Student)
                .HasForeignKey(r => r.StudentId);

            modelBuilder.Entity<User>(user =>
            {
                user.Property(u => u.Username).IsRequired();
                user.HasIndex(u => u.Username).IsUnique();
                user.Property(u => u.PasswordHash).IsRequired();
                user.Property(u => u.FullName).IsRequired();

                // Email אינו IsRequired: לתלמידות אין מייל, ולכל השורות שקיימות היום אין.
                // האינדקס הייחודי עדיין נכון — SQLite מתייחס ל-NULL-ים כשונים זה מזה, כך
                // שאין-סוף שורות בלי מייל חיות זו לצד זו, ובכל זאת מייל אחד לא יכול להוביל
                // לשני חשבונות כששחזור הסיסמה יחפש לפיו.
                user.HasIndex(u => u.Email).IsUnique();

                user.Property(u => u.Role).HasConversion<string>();
            });

            modelBuilder.Entity<PasswordResetToken>(token =>
            {
                token.Property(t => t.TokenHash).IsRequired();

                // כל אימות של קישור הוא חיפוש לפי הגיבוב הזה, ולכן אינדקס. ייחודי — שני
                // טוקנים לא אמורים להתגבב לאותו ערך, ואם זה קורה עדיף להיכשל בכתיבה
                // מאשר ש-FirstOrDefault יבחר שרירותית לאיזו משתמשת הקישור שייך.
                token.HasIndex(t => t.TokenHash).IsUnique();

                // Cascade: לטוקן אין משמעות בלי המשתמשת. שאר השרשראות במערכת הן Restrict
                // כדי להגן על עבודת תלמידות — כאן אין מה להגן עליו, וטוקן יתום היה
                // מצביע על שורה שאינה קיימת.
                token.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            modelBuilder.Entity<SchoolClass>(cls =>
            {
                cls.Property(c => c.Name).IsRequired().HasMaxLength(50);
                cls.HasIndex(c => new { c.Name, c.AcademicYear }).IsUnique();
            });

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // many-to-many עם skip navigations — EF יוצר טבלת קישור LessonSchoolClass
            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Classes)
                .WithMany(c => c.Lessons)
                .UsingEntity(j => j.ToTable("LessonSchoolClasses"));

            // בעלות מורה על שיעור + קורס — Restrict בכוונה: cascade היה מוחק Assignments → Submissions → עבודת תלמידים
            modelBuilder.Entity<Lesson>().HasOne(l => l.Teacher).WithMany()
                .HasForeignKey(l => l.TeacherId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lesson>().HasOne(l => l.Course).WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Course>(c =>
            {
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
                c.HasIndex(x => new { x.TeacherId, x.Name }).IsUnique();
            });
            modelBuilder.Entity<Lesson>().HasIndex(l => l.TeacherId);
            modelBuilder.Entity<Lesson>().HasIndex(l => l.CourseId);
        }

    }
}
