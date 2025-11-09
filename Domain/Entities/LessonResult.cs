namespace Domain.Entities
{
    public class LessonResult
    {
        public int Id { get; private set; }
        public int StudentId { get; private set; }
        public int LessonId { get; private set; }
        public double FinalScore { get; set; }
        public bool IsComplete { get; set; }
        public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Lesson Lesson { get; set; }
    }
}


