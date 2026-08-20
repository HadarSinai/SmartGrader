namespace SmartGrader.Application.Dtos.Assignments
{
    public class UpdateAssignmentRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBonus { get; set; }
        public double BonusValue { get; set; }
        public string? MethodName { get; init; }
        public string GradingMode { get; set; } = "FullProgram";

        public List<TestCaseDto> Tests { get; set; } = new();
        public List<ExpectedFileDto> ExpectedFiles { get; set; } = new();

        /// <summary>הפתרון לדוגמה של המורה — אופציונלי. ר' Assignment.ReferenceSolution.</summary>
        public List<ReferenceSolutionFileDto> ReferenceSolution { get; set; } = new();

        /// <summary>הדרישות המבניות. ר' CreateAssignmentRequestDto.StructuralRules.</summary>
        public List<StructuralRuleDto> StructuralRules { get; set; } = new();

        /// <summary>כמה מ-100 הנקודות מוקצות למקרי הבדיקה. 0 חוקי (תרגיל מחלקות).</summary>
        public int TestsAllocation { get; set; } = Domain.Entities.Assignment.TotalPoints;

        /// <summary>הציון שמתחתיו התלמידה רשאית להגיש שוב.</summary>
        public int RetryThreshold { get; set; } = Domain.Entities.Assignment.DefaultRetryThreshold;
    }
}
