namespace SmartGrader.Domain.Entities
{
    public class LessonResult
    {
        public int Id { get; private set; }
        public int StudentId { get; private set; }
        public int LessonId { get; private set; }
        public double? FinalScore { get; private set; }
        public bool IsComplete { get; private set; }
        public DateTime? CalculatedAt { get; private set; } = DateTime.UtcNow;

        protected LessonResult() { }
        public static LessonResult Create(int studentId, int lessonId)
        {
            if (studentId <= 0) throw new ArgumentException("Invalid student id.", nameof(studentId));
            if (lessonId <= 0) throw new ArgumentException("Invalid lesson id.", nameof(lessonId));
            return new LessonResult { StudentId = studentId, LessonId = lessonId };
        }
        public void CompleteWith(double score, bool hasBonus = false)
        {
            if (IsComplete) throw new InvalidOperationException("Already completed.");
            // עם בונוס הציון יכול להגיע עד 150, בלי בונוס עד 100
            double maxScore = hasBonus ? 150 : 100;
            if (score < 0 || score > maxScore)
                throw new ArgumentOutOfRangeException(
                    nameof(score),
                    $"Score must be between 0 and {maxScore} (hasBonus: {hasBonus}).");

            FinalScore = score;
            IsComplete = true;
            CalculatedAt = DateTime.UtcNow;
        }

        public Student Student { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;

    }
}


