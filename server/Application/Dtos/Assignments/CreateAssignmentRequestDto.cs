namespace SmartGrader.Application.Dtos.Assignments
{
    public class CreateAssignmentRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public string MethodName { get; init; } = "";

        // GradingMode כ-string בדיוק כמו SubmissionResponseDto.Status — נבדק ע"י FluentValidation
        // מול שמות ה-enum SmartGrader.Domain.Entities.GradingMode לפני שהוא ממופה.
        public string GradingMode { get; set; } = "FullProgram";

        public List<TestCaseDto> Tests { get; set; } = new();
        public List<ExpectedFileDto> ExpectedFiles { get; set; } = new();
    }
}
