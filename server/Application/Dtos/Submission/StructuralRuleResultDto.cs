namespace SmartGrader.Application.Dtos.Submissions
{
    /// <summary>
    /// תוצאת דרישה מבנית אחת, כפי שהיא מוצגת בטבלת הדרישות.
    /// <para>
    /// ⚠️ בניגוד למקרי בדיקה, <b>אין כאן מה להסתיר מהתלמידה</b>: הדרישה נכתבה בניסוח המטלה
    /// מלכתחילה ("חובה רקורסיה"), והידיעה שהיא לא התקיימה היא בדיוק מה שהתלמידה צריכה כדי
    /// לתקן. ר' <c>TestVisibility</c> להבחנה מול הטסטים.
    /// </para>
    /// </summary>
    public class StructuralRuleResultDto
    {
        /// <summary>הדרישה בעברית — "חובה להשתמש ברקורסיה", "לכל היותר 3 if".</summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>מה נמצא בפועל — "לא נמצאה רקורסיה בקוד", "נמצאו 4 מופעים של if (בשורות 3, 7)".</summary>
        public string Finding { get; set; } = string.Empty;

        public bool Passed { get; set; }

        /// <summary><c>Blocking</c> · <c>Scored</c> · <c>Advisory</c>.</summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>נקודות — רק לדרישה מנוקדת. 0 לחוסמת ולהמלצה, שאינן נושאות ניקוד.</summary>
        public int Points { get; set; }

        public int ExpectedCount { get; set; }
        public int ActualCount { get; set; }

        /// <summary>שורות שבהן נמצאו המופעים, כדי שהמשוב יוכל להצביע על מקום בקוד.</summary>
        public List<int> Lines { get; set; } = new();
    }
}
