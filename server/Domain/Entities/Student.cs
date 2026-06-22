namespace SmartGrader.Domain.Entities
{
    public class Student
    {
        public int Id { get; private set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
       public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        protected Student() { }

        // קשרים
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public ICollection<LessonResult> LessonResults { get; set; } = new List<LessonResult>();
    }
}

