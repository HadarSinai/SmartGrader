namespace SmartGrader.Application.Dtos.Assignments
{
    public class UpdateAssignmentRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        

        public List<TestCaseDto> Tests { get; set; } = new();
    }
}
