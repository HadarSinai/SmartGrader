namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// קובץ מתוך הפתרון לדוגמה של המורה — המימוש הידוע-כתקין שמקרי הבדיקה נבדקים מולו.
    /// <para>
    /// ⚠️ זו התשובה המלאה לתרגיל. לעולם לא נשלח לתלמידה, בשום DTO ובשום נתיב —
    /// ר' <c>TestVisibility.RedactReferenceSolution</c>.
    /// </para>
    /// <para>
    /// רשימה ולא מחרוזת אחת, במקביל ל-<see cref="ExpectedFile"/>: תרגיל רב-קובצי נפתר בכמה
    /// מחלקות, והמורה מדביקה כל אחת בשורה משלה. במסלולי קובץ יחיד יש כאן פריט אחד.
    /// </para>
    /// </summary>
    public class ReferenceSolutionFile
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
