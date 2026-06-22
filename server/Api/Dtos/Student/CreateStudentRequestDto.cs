namespace Api.Dtos.Student
{
    public class CreateStudentRequestDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // אופציונלי – למשל לספירת הגשות ותוצאות שיעורים
        public int SubmissionsCount { get; set; }
        public int LessonResultsCount { get; set; }
    }
}
