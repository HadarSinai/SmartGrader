namespace SmartGrader.Application.Dtos.Student
{
    public class StudentResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int SubmissionsCount { get; set; }
        public int LessonResultsCount { get; set; }
    }
}
