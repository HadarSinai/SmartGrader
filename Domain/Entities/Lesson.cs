namespace SmartGrader.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; private set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public DateTime LessonDate { get; set; }
        public string TeacherName { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
