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

        /// <summary>הפתרון לדוגמה של המורה — אופציונלי. ר' Assignment.ReferenceSolution.</summary>
        public List<ReferenceSolutionFileDto> ReferenceSolution { get; set; } = new();

        /// <summary>
        /// הדרישות המבניות. ⚠️ <b>אלה אינן תוספת אלא כלי הניקוד המרכזי בקורס הזה</b>: רוב
        /// התרגילים מקודדים את הנתונים בתוך הקוד, ולכן יש להם מקרה בדיקה יחיד שהניקוד עליו
        /// בינארי בהכרח. ר' "The single-test shape is the common case" בתוכנית.
        /// </summary>
        public List<StructuralRuleDto> StructuralRules { get; set; } = new();

        /// <summary>
        /// כמה מ-100 הנקודות מוקצות למקרי הבדיקה. 0 חוקי — תרגיל מחלקות מנוקד על המבנה בלבד.
        /// </summary>
        public int TestsAllocation { get; set; } = Domain.Entities.Assignment.TotalPoints;

        /// <summary>הציון שמתחתיו התלמידה רשאית להגיש שוב, בלי הגבלת ניסיונות.</summary>
        public int RetryThreshold { get; set; } = Domain.Entities.Assignment.DefaultRetryThreshold;
    }
}
