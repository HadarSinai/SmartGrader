namespace SmartGrader.Application.Dtos.Lessons
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public string LessonDateHebrew { get; set; } = string.Empty;
        public int HebrewYear { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewDay { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // אופציונלי – אם תרצי להציג כמה משימות יש לשיעור
        public int AssignmentsCount { get; set; }
    }
}
