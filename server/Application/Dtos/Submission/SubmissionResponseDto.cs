namespace SmartGrader.Application.Dtos.Submissions
{
    public class SubmissionResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }

        // השיעור שמתחת לתרגיל. בלעדיו הלקוח חייב לגרור lessonId ב-queryParam ממסך למסך —
        // ובכל מסלול שלא עובר דרך רשימת התרגילים (למשל "הציונים שלי") הוא פשוט חסר.
        public int LessonId { get; set; }

        public string SourceCode { get; set; } = string.Empty;
        public List<SubmissionFileDto> SourceFiles { get; set; } = new();

        public double? Score { get; set; }
        public AiFeedbackResultDto? Feedback { get; set; }
        public List<TestCaseResultDto> TestResults { get; set; } = new();

        public string Status { get; set; } = string.Empty;
        public string? AiError { get; set; }
        public string? CompileError { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string? StudentName { get; set; }
        public string? AssignmentName { get; set; }
    }
}
