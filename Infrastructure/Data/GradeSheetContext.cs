using SmartGrader.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SmartGrader.Application.Common.Exceptions;

namespace Infrastructure.Data
{
    public class GradeSheetContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<LessonResult> LessonResults { get; set; }
        public DbSet<Log> Logs { get; set; }

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

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Assignments)
                .WithOne(a => a.Lesson)
                .HasForeignKey(a => a.LessonId);

            modelBuilder.Entity<Assignment>()
                .HasMany(a => a.Submissions)
                .WithOne(s => s.Assignment)
                .HasForeignKey(s => s.AssignmentId);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.LessonResults)
                .WithOne(r => r.Student)
                .HasForeignKey(r => r.StudentId);
        }

        // 👇 זה החלק החדש – טיפול בשגיאת UNIQUE
        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // בודקים אם השגיאה קשורה ל-UNIQUE / constraint
                var message = ex.InnerException?.Message ?? ex.Message;

                if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    // זורקים שגיאה לוגית שלנו – המידלוור יתפוס ויחזיר 409
                    throw new UniqueConstraintException("Duplicate value – record already exists.");
                }

                // אם זו לא שגיאת UNIQUE – ממשיכים כרגיל
                throw;
            }
        }
    }
}
