namespace WebApplication2.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public DateTime namespace WebApplication2.Entities
    {
        public class Lesson
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime LessonDate { get; set; }
            public string TeacherName { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // קשרים
            public ICollection<Assignment> Assignments { get; set; }
        }
    }
 { get; set; } = DateTime.UtcNow;

// קשרים
public ICollection<Submission> Submissions { get; set; }
public ICollection<LessonResult> LessonResults { get; set; }
    }
}

