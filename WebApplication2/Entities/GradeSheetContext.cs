using Microsoft.EntityFrameworkCore;
using SmartGrader.Entities;

namespace SmartGrader
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

            // קשרים בין טבלאות
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
    }
}


