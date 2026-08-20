namespace SmartGrader.Application.Dtos.Assignments
{
    public class TestCaseDto
    {
        public string Input { get; set; }
        public string Expected { get; set; }

        /// <summary>מקרה דוגמה — מוצג לתלמידה. מקרה שאינו דוגמה מסונן בשרת ולא נשלח כלל.</summary>
        public bool IsSample { get; set; }

        /// <summary>
        /// מקרה ליבה מול מקרה קצה. ר' <c>TestCase.IsCore</c> — היום לא משפיע על הציון,
        /// ברירת המחדל <c>true</c>.
        /// </summary>
        public bool IsCore { get; set; } = true;
    }
}
