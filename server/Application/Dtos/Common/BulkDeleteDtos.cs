namespace SmartGrader.Application.Dtos.Common
{
    /// <summary>
    /// המזהים שנבחרו למחיקה במסך רשימה.
    /// <para>
    /// ⚠️ רשימת מזהים ולא "מחקי הכול לפי סינון": סינון שנשלח לשרת נפתר שם מחדש, ומה שיימחק
    /// אינו בהכרח מה שהמורה ראתה על המסך כשלחצה.
    /// </para>
    /// </summary>
    public class BulkDeleteRequestDto
    {
        public List<int> Ids { get; set; } = new();
    }

    /// <summary>
    /// תוצאת מחיקה מרובה. <b>הצלחה חלקית היא התוצאה הרגילה</b>, לא מקרה קצה: בחירה של
    /// עשר שורות שבחמש מהן יש עבודת תלמידות מוחקת חמש ומסרבת לחמש, ואומרת על כל אחת למה.
    /// </summary>
    public class BulkDeleteResultDto
    {
        /// <summary>מה שנמחק בפועל.</summary>
        public List<int> DeletedIds { get; set; } = new();

        /// <summary>מה שסורב, ומדוע. ריק = הכול נמחק.</summary>
        public List<BulkDeleteFailureDto> Failures { get; set; } = new();

        public int DeletedCount => DeletedIds.Count;
        public int FailedCount => Failures.Count;
    }

    public class BulkDeleteFailureDto
    {
        public int Id { get; set; }

        /// <summary>
        /// הסיבה, כלשונה מהמחיקה הבודדת.
        /// <para>
        /// ⚠️ בלי שם הישות בכוונה: המסך מחזיק את השורות ויודע לתרגם מזהה לשם, ומשיכת
        /// הישות רק כדי לייצר תווית הייתה שאילתה נוספת לכל שורה שסורבה.
        /// </para>
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
