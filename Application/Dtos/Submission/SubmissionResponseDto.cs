namespace SmartGrader.Application.Dtos.Submissions
{
    public class SubmissionResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool CheckedByAI { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }

        // אופציונלי – אם תרצי להציג פרטים נוספים
        public string? StudentName { get; set; }   // לא חובה
        public string? AssignmentName { get; set; } // לא חובה
    }
}
