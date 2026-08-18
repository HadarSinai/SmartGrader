namespace SmartGrader.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; private set; }
        public string Subject { get; set; }
        public DateTime LessonDate { get; set; }
        public int TeacherId { get; set; }
        public User Teacher { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        protected   Lesson() { }

        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
    }
}
