namespace SmartGrader.Api.Dtos.Lessons
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // אופציונלי – אם תרצי להציג כמה משימות יש לשיעור
        public int AssignmentsCount { get; set; }
    }
}
