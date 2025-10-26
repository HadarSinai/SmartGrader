namespace SmartGrader.Entities
{
    public class LessonResult
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public double FinalScore { get; set; }
        public bool IsComplete { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // קשרים
        public Student Student { get; set; }
        public Lesson Lesson { get; set; }
    }
}


