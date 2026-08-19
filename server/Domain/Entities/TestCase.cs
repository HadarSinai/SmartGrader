namespace SmartGrader.Domain.Entities
{
    public class TestCase
    {
        public string Input { get; set; }
        public string Expected { get; set; }

        /// <summary>
        /// מקרה "דוגמה" — היחיד שהתלמידה רואה. ברירת המחדל false היא הגנה מכוונת (fail closed):
        /// מקרי בדיקה קיימים ב-TestsJson נטענים בלי המאפיין הזה ולכן הופכים למוסתרים מאליהם,
        /// והמורה מסמנת דוגמאות מכאן והלאה. מוסתר = הקלט והפלט הצפוי לעולם לא נשלחים לתלמידה,
        /// גם לא אחרי הבדיקה. ר' TestVisibility.
        /// </summary>
        public bool IsSample { get; set; }
    }
}
