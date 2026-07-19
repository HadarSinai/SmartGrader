namespace SmartGrader.Domain.Entities
{
    public class SchoolClass
    {
        public int Id { get; private set; }
        public string Name { get; set; }

        // שנה עברית כמספר (למשל 5786 = תשפ"ו)
        public int AcademicYear { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        protected SchoolClass() { }

        public static SchoolClass Create(string name, int academicYear)
            => new SchoolClass { Name = name, AcademicYear = academicYear, IsArchived = false };

        // קשרים
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
