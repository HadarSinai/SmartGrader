namespace SmartGrader.Application.Dtos.Submissions
{
    public class UpdateSubmissionRequestDto
    {
        public string SourceCode { get; set; } = string.Empty;
        //אולי לא
        public double Score { get; set; }
        //אולי לא
        public bool CheckedByAI { get; set; }

        public string Comments { get; set; } = string.Empty;
    }
}
