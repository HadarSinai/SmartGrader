namespace SmartGrader.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
       public DateTime CreateAt { get; set; } = DateTime.UtcNow;

// קשרים
public ICollection<Submission> Submissions { get; set; }
public ICollection<LessonResult> LessonResults { get; set; }
    }
}

