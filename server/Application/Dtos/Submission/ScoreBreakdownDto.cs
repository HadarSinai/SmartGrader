namespace SmartGrader.Application.Dtos.Submissions
{
    /// <summary>
    /// פירוק הציון כפי שהוא מוצג — "בדיקות 64 · דרישות 0 · סה"כ 64".
    /// <para>
    /// מחליף את ארבעת אריחי הציון שה-AI ניקד בעצמו. כאן כל מספר הוא תוצאה של חישוב
    /// דטרמיניסטי שאפשר לשחזר, ולכן אפשר גם להתווכח איתו מול הקוד.
    /// </para>
    /// </summary>
    public class ScoreBreakdownDto
    {
        public double TestPoints { get; set; }
        public double RulePoints { get; set; }
        public double Total { get; set; }

        public int TestsAllocation { get; set; }
        public int RulesAllocation { get; set; }

        public int PassedTests { get; set; }
        public int TotalTests { get; set; }

        /// <summary>
        /// האם כל מקרי הליבה עברו. <c>false</c> מאפס את נקודות הבדיקות — הפתרון לא עשה את
        /// הדבר המרכזי שהתרגיל ביקש, ומקרי קצה שעברו במקרה אינם מזכים בנקודות.
        /// </summary>
        public bool AllCorePassed { get; set; }
    }
}
