namespace SmartGrader.Application.Dtos.Assignments
{
    public class TestCaseDto
    {
        public string Input { get; set; }
        public string Expected { get; set; }

        /// <summary>מקרה דוגמה — מוצג לתלמידה. מקרה שאינו דוגמה מסונן בשרת ולא נשלח כלל.</summary>
        public bool IsSample { get; set; }
    }
}
