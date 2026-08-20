using System.Text.Json.Serialization;

namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// קטלוג המבנים שאפשר לדרוש בקוד. מסודר לפי סדר ההוראה בכיתה, לא לפי אלפבית —
    /// הרשימה הזו מוצגת למורה כפי שהיא.
    /// <para>
    /// <b>הוספת מבנה = ערך אחד כאן + <c>case</c> אחד ב-<c>RoslynCodeAnalysisService</c>.</b>
    /// הפתיחות הזו היא הדרישה עצמה: מלמדים מבנה אחר כל שבוע.
    /// </para>
    /// <para>
    /// ⚠️ מסודר כמחרוזת ב-JSON — ר' ההסבר ב-<see cref="RuleKind"/>. הערכים המספריים כאן
    /// אינם חלק מהחוזה ומותר להוסיף באמצע.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CodeConstruct
    {
        // ── תנאים ──
        If = 0,
        Switch = 1,
        Ternary = 2,

        // ── לולאות ──
        For = 10,
        While = 11,
        DoWhile = 12,
        Foreach = 13,

        /// <summary>לולאה מכל סוג — סכום כל ארבע הלולאות.</summary>
        AnyLoop = 14,

        // ── מתודות ──
        Method = 20,

        /// <summary>מתודה שגוף שלה קורא לעצמה. ר' האזהרה על השוואת מזהים ב-RoslynCodeAnalysisService.</summary>
        Recursion = 21,

        // ── אוספים ──
        Array = 30,

        /// <summary>
        /// מערך דו-ממדי ומעלה (<c>int[,]</c>).
        /// ⚠️ צורת צומת שונה מ-<see cref="Array"/> ואסור שתתמזג אליה — "חובה מטריצה" מול קוד
        /// שמצהיר רק <c>int[]</c> חייב להיכשל.
        /// </summary>
        Matrix = 31,
        List = 32,
        Dictionary = 33,

        // ── סוגי משתנים ──
        BoolVariable = 40,
        StringVariable = 41,
        CharVariable = 42,
        LocalVariable = 43,
        Constant = 44,

        // ── מונחה עצמים ──
        // הקבוצה הזו קיימת מפני שלתרגיל מחלקות אין קלט ואין פלט כלל, ובלעדיה אי אפשר
        // לנסח אותו בכלל. ר' "תרגילים בלי קלט/פלט" בתוכנית.
        Class = 50,
        Property = 51,
        Constructor = 52,
        Field = 53,
        Inheritance = 54,
        Interface = 55,

        // ── מתקדם ──
        TryCatch = 60,
        Linq = 61,

        // ── בקרת זרימה ──
        Break = 70,
        Continue = 71,
        Goto = 72,

        // ── יעילות ──
        /// <summary>
        /// עומק הקינון המרבי של לולאות. יעילות אינה רכיב ניקוד נפרד — היא דרישה מנוקדת
        /// שמשתמשת במבנה הזה.
        /// </summary>
        NestedLoopDepth = 80
    }
}
