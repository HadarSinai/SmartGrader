
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
        public DbSet<LessonResult> LessonResults { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        public DbSet<Course> Courses { get; set; }

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
                user.Property(u => u.Role).HasConversion<string>();
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
