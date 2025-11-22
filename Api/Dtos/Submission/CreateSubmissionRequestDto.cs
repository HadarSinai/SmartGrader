namespace SmartGrader.Api.Dtos.Submissions
{
    public class CreateSubmissionRequestDto
    {
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}
