namespace SmartGrader.Application.Common.Validation
{
    /// <summary>
    /// תחימת בקשת ההצעות. כל לחיצה היא קריאת API בתשלום, ולכן שני הגבולות כאן אינם קוסמטיים:
    /// <list type="bullet">
    /// <item><b>העלות</b> — <c>count</c> ללא תקרה הוא בקשה פתוחה לחיוב פתוח.</item>
    /// <item><b>הסקירה</b> — רשימת הצעות שאי אפשר לעבור עליה במבט אחד מזמינה אישור גורף,
    /// וזה בדיוק מה שהתכונה הזו נועדה למנוע: כל שורה אמורה להיקרא בעיניים.</item>
    /// </list>
    /// הגבלת הקצב עצמה (כמה לחיצות בדקה) יושבת ב-<c>Program.cs</c>, מדיניות "ai".
    /// </summary>
    public static class SuggestTestCasesLimits
    {
        public const int MinCount = 1;
        public const int MaxCount = 10;

        /// <summary>אורך התיאור שנשלח למודל. תיאור ארוך מזה נחתך ולא נדחה — ר' ההערה ב-Handler.</summary>
        public const int MaxDescriptionLength = 4000;
    }
}
