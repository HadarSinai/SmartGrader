namespace SmartGrader.Application.Dtos.Submissions
{
    public class TestCaseResultDto
    {
        public string Input { get; set; } = string.Empty;
        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? Error { get; set; }
    }
}
