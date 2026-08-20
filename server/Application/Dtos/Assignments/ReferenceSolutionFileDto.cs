namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// קובץ מתוך הפתרון לדוגמה של המורה.
    /// ⚠️ התשובה המלאה לתרגיל — נחתך בשרת לפני שה-DTO מגיע לתלמידה. ר' TestVisibility.
    /// </summary>
    public class ReferenceSolutionFileDto
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
