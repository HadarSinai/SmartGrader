namespace SmartGrader.Application.Services.Notifications
{
    /// <summary>
    /// מה נחשב "הרבה תלמידות". מוגדר <b>במקום אחד</b> ולא כמספר שחוזר בארבע האגרגציות —
    /// אחרת "הרבה" הופך לארבעה כללים שונים שאף אחד לא זוכר להשוות ביניהם.
    /// <para>נטען מ-<c>Notifications:ClassSignals</c> ב-appsettings.</para>
    /// </summary>
    public class ClassSignalThresholds
    {
        /// <summary>
        /// מספר התלמידות המזערי. ⚠️ זה מה שמונע מכיתה של ארבע לייצר התראה על כל תקלה —
        /// בלי מינימום מוחלט, 2 מתוך 3 הוא 67% ועובר כל סף יחסי.
        /// </summary>
        public int MinAffectedStudents { get; set; } = 3;

        /// <summary>החלק היחסי המזערי מתוך <i>מי שהגישה</i>, לא מתוך גודל הכיתה.</summary>
        public double MinAffectedRatio { get; set; } = 0.5;

        /// <summary>
        /// כמה הגשות צריכות להיות לפני שאפשר לומר "אף אחת לא עברה".
        /// ⚠️ בלי זה, ההגשה הראשונה בכיתה שנכשלת מכריזה שהתרגיל שבור.
        /// </summary>
        public int MinSubmissionsForNobodyPassed { get; set; } = 3;

        /// <summary>הכלל היחיד שמכריע "הרבה" — סיגנלים 1, 2 ו-4 עוברים דרכו.</summary>
        public bool IsMany(int affected, int total) =>
            total > 0
            && affected >= MinAffectedStudents
            && (double)affected / total >= MinAffectedRatio;
    }
}
