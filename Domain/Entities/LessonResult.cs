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
         {  if (studentId <= 0) throw new ArgumentException("Invalid student id.", nameof(studentId));
        if (lessonId  <= 0) throw new ArgumentException("Invalid lesson id.",  nameof(lessonId));
           return new LessonResult{ StudentId = studentId, LessonId = lessonId };}
        public void CompleteWith(double score)
        {
            if (IsComplete) throw new InvalidOperationException("Already completed.");
            if (score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(score));

            FinalScore = score;
            IsComplete = true;
            CalculatedAt = DateTime.UtcNow;
        }

        // קשרים
        public Student Student { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;
    }
}


