namespace Api.Dtos.Student
{
    public class StudentResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // אופציונלי – אם תרצי להציג כמה הגשות וכמה תוצאות שיעור יש לתלמיד
        public int SubmissionsCount { get; set; }
        public int LessonResultsCount { get; set; }
    }
}
