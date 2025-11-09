namespace Domain.Entities
{
    public class Student
    {
        public int Id { get; private set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
       public DateTime CreateAt { get; private set; } = DateTime.UtcNow;

// קשרים
public ICollection<Submission> Submissions { get; set; }
public ICollection<LessonResult> LessonResults { get; set; }
    }
}

