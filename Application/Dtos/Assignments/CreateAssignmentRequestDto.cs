namespace SmartGrader.Application.Dtos.Assignments
{
    public class CreateAssignmentRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public string FunctionName { get; set; }
        public string ReturnType { get; set; }
        public string TestsJson { get; set; }
    }
}
