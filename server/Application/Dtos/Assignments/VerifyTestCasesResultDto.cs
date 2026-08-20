namespace SmartGrader.Application.Dtos.Assignments
{
    /// <summary>
    /// תוצאת אימות של מקרה בדיקה בודד מול הפתרון לדוגמה.
    /// </summary>
    public class TestCaseVerificationDto
    {
        /// <summary>מיקום המקרה ברשימה שנשלחה — הלקוח משתמש בו כדי לכתוב את התיקון לשורה הנכונה.</summary>
        public int Index { get; set; }

        public string Input { get; set; } = "";

        /// <summary>מה שהמורה הקלידה בשדה "פלט צפוי".</summary>
        public string Expected { get; set; } = "";

        /// <summary>מה שהפתרון של המורה החזיר בפועל. זה הערך שכפתור "תיקון" כותב.</summary>
        public string Actual { get; set; } = "";

        public bool Passed { get; set; }

        /// <summary>stderr / שגיאת ריצה, אם הייתה.</summary>
        public string? Error { get; set; }

        /// <summary>"Accepted" / "Wrong Answer" / "Runtime Error (NZEC)" וכו' מ-Judge0.</summary>
        public string? StatusDescription { get; set; }

        /// <summary>
        /// האם מותר להציע "תיקון" לשורה הזו. שגיאת ריצה או חריגת זמן מחזירות פלט ריק —
        /// כתיבת הריק הזה לשדה "פלט צפוי" הייתה הופכת מקרה בדיקה תקין למקרה שמצפה לכלום.
        /// </summary>
        public bool CanFix { get; set; }
    }

    /// <summary>
    /// תוצאת הרצת הפתרון לדוגמה מול כל מקרי הבדיקה. <b>שום דבר מכאן לא נשמר</b> —
    /// אין Submission, אין ציון, אין קריאה ל-AI. זה תיאור של הטיוטה של המורה, לא של עבודה מנוקדת.
    /// </summary>
    public class VerifyTestCasesResultDto
    {
        public int Passed { get; set; }
        public int Total { get; set; }

        /// <summary>
        /// ⚠️ כשל קומפילציה כאן הוא באג <b>בפתרון של המורה</b>, לא במקרי הבדיקה. הלקוח מציג
        /// את זה בנפרד לגמרי מרשימת המקרים — אחרת המורה מחפשת את הבעיה בטבלה הלא נכונה.
        /// </summary>
        public bool HasCompileError { get; set; }
        public string? CompileError { get; set; }

        public List<TestCaseVerificationDto> Results { get; set; } = new();
    }
}
