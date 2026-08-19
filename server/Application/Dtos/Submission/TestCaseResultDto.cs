namespace SmartGrader.Application.Dtos.Submissions
{
    public class TestCaseResultDto
    {
        public string Input { get; set; } = string.Empty;
        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? Error { get; set; }

        /// <summary>נשמר על התוצאה בזמן הבדיקה — ר' TestCaseResult. false = מקרה מוסתר.</summary>
        public bool IsSample { get; set; }

        /// <summary>
        /// true כשהפרטים של השורה הוסתרו מהקורא (מקרה בדיקה שאינו דוגמה, בתצוגת תלמידה).
        /// אז Input/Expected/Actual/Error ריקים ורק Passed אמיתי. ר' TestVisibility.
        /// </summary>
        public bool IsHidden { get; set; }
    }
}
