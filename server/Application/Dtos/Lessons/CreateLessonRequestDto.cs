namespace SmartGrader.Application.Dtos.Lessons
{
    public class CreateLessonRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int HebrewYear { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewDay { get; set; }
        public string TeacherName { get; set; } = string.Empty;
    }
}
