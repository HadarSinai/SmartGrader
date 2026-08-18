namespace SmartGrader.Application.Dtos.Lessons
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public string LessonDateHebrew { get; set; } = string.Empty;
        public int HebrewYear { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewDay { get; set; }
        public DateTime CreatedAt { get; set; }

        // אופציונלי – אם תרצי להציג כמה משימות יש לשיעור
        public int AssignmentsCount { get; set; }

        // הכיתות שהשיעור משויך אליהן
        public List<LessonClassDto> Classes { get; set; } = new();
        public string ClassNames { get; set; } = string.Empty;
    }

    public class LessonClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
