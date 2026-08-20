namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// בקשת אימות מקרי הבדיקה מול הפתרון לדוגמה.
    /// <para>
    /// ⚠️ כל מה שנדרש להרצה מגיע <b>בגוף הבקשה</b> ולא נקרא מהתרגיל השמור, כי הטופס עדיין
    /// לא נשמר: המורה מאמתת בזמן הכתיבה — לפעמים על תרגיל שעוד לא קיים ב-DB בכלל. לכן גם
    /// אין כאן <c>AssignmentId</c>, רק <c>LessonId</c> מהנתיב לצורך בדיקת בעלות.
    /// </para>
    /// </summary>
    public class VerifyTestCasesRequestDto
    {
        public string GradingMode { get; set; } = "FullProgram";
        public string? MethodName { get; set; }

        public List<ReferenceSolutionFileDto> ReferenceSolution { get; set; } = new();
        public List<ExpectedFileDto> ExpectedFiles { get; set; } = new();
        public List<TestCaseDto> Tests { get; set; } = new();
    }
}
