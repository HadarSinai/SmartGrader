namespace SmartGrader.Domain.Entities
{
    public class Assignment
    {
        public int Id { get; private set; }
        public int LessonId { get; private set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        // connections
        public Lesson Lesson { get; set; }
        public ICollection<Submission> Submissions { get; set; }
    }
}

       
