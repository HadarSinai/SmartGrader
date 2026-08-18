namespace SmartGrader.Application.Dtos.Lessons
{
    public class UpdateLessonRequestDto
    {
        public int CourseId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int HebrewYear { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewDay { get; set; }
        public List<int> ClassIds { get; set; } = new();
    }
}
