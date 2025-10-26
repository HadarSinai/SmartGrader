namespace SmartGrader.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public DateTime LessonDate { get; set; }
        public string TeacherName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // קשרים
        public ICollection<Assignment> Assignments { get; set; }
    }
}
