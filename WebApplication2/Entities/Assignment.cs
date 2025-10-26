namespace SmartGrader.Entities
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

       
