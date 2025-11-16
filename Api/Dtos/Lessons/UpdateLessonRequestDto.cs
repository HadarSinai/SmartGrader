namespace SmartGrader.Api.Dtos.Lessons
{
    public class UpdateLessonRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
    }
}
