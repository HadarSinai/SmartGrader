namespace SmartGrader.Application.Dtos.Submissions
{
    public class AiFeedbackResultDto
    {
        public List<string> Good { get; set; } = new();
        public AiFeedbackIssuesDto Issues { get; set; } = new();
        public List<string> MinimalChanges { get; set; } = new();
        public string? OptionalFullSolution { get; set; }
        public AiFeedbackScoresDto Scores { get; set; } = new();

        // כאשר false, RawResponse מכיל את טקסט המשוב הגולמי (לא פורש) לתצוגת גיבוי בקליינט.
        public bool ParseSucceeded { get; set; } = true;
        public string? RawResponse { get; set; }
    }

    public class AiFeedbackIssuesDto
    {
        public List<string> Correctness { get; set; } = new();
        public List<string> Readability { get; set; } = new();
        public List<string> Performance { get; set; } = new();
    }

    public class AiFeedbackScoresDto
    {
        public double? TestScore { get; set; }
        public double? CodeQualityScore { get; set; }
        public double? EfficiencyScore { get; set; }
        public double? FinalScore { get; set; }
    }
}
