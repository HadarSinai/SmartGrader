namespace SmartGrader.Domain.Entities
{
    public class Course
    {
        public int Id { get; private set; }
        public string Name { get; set; }
        public int TeacherId { get; set; }
        public User Teacher { get; set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        protected Course() { }

        public static Course Create(string name, int teacherId)
            => new Course { Name = name, TeacherId = teacherId };

        // קשרים
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
