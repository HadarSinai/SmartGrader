namespace SmartGrader.Application.Dtos.Courses
{
    public class CourseResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LessonsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
