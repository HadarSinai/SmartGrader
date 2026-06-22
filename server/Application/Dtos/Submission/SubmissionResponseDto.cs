namespace SmartGrader.Application.Dtos.Submissions
{
    public class SubmissionResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }

        public string SourceCode { get; set; } = string.Empty;

        public double? Score { get; set; }
        public string? Comments { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? AiError { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string? StudentName { get; set; }
        public string? AssignmentName { get; set; }
    }
}
