namespace WebApplication2.Entities
{
    public class Assignment
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // קשרים
        public Lesson Lesson { get; set; }
        public ICollection<Submission> Submissions { get; set; }
    }
}
namespace WebApplication2.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public string SourceCode { get; set; }
        public double Score { get; set; }
        public bool CheckedByAI { get; set; }
        public string Comments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Assignment Assignment { get; set; }
    }
}
