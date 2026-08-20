namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// בקשה ל-AI להציע מקרי בדיקה. כמו באימות — הכל מגיע מהטופס ולא מתרגיל שמור,
    /// כי ההצעה נועדה בדיוק לרגע שבו התרגיל עוד נכתב.
    /// </summary>
    public class SuggestTestCasesRequestDto
    {
        /// <summary>תיאור המשימה — זה מה שהמודל מקבל לעבוד איתו. בלעדיו אין מה להציע.</summary>
        public string Description { get; set; } = "";

        public string GradingMode { get; set; } = "FullProgram";
        public string? MethodName { get; set; }

        /// <summary>כמה מקרים לבקש. תחום ב-<c>SuggestTestCasesLimits</c>.</summary>
        public int Count { get; set; } = 5;

        /// <summary>אופציונלי — בלעדיו ההצעות חוזרות מסומנות "לא אומת".</summary>
        public List<ReferenceSolutionFileDto> ReferenceSolution { get; set; } = new();
        public List<ExpectedFileDto> ExpectedFiles { get; set; } = new();
    }
}
