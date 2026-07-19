namespace SmartGrader.Application.Dtos.Student
{
    public class StudentResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool ClassIsArchived { get; set; }
        public DateTime CreatedAt { get; set; }

        public int SubmissionsCount { get; set; }
        public int LessonResultsCount { get; set; }
        public bool HasAccount { get; set; }
    }
}
