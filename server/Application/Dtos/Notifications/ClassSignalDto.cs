using System.Text.Json.Serialization;

namespace SmartGrader.Application.Dtos.Notifications
{
    /// <summary>
    /// סוג הסיגנל. שני הראשונים מספרים משהו <b>על הכיתה</b> (מה ללמד מחדש), שני האחרונים
    /// משהו <b>על התרגיל</b> (שהמורה כתבה אותו לא נכון).
    /// <para>
    /// ⚠️ ההבחנה אינה קוסמטית: בלי 3–4 מורה מסיקה שהתלמידות נכשלו, בזמן שבפועל הפלט הצפוי
    /// או חתימת המתודה שגויים.
    /// </para>
    /// <para>
    /// מסודר כמחרוזת ב-JSON מאותו נימוק כמו <c>RuleKind</c>: הקטלוג ייגדל (ר' "מחוץ לתכולה"
    /// ב-plan-teacherNotifications), והלקוח לא אמור להישבר מהוספת ערך באמצע.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClassSignalType
    {
        /// <summary>דרישה מבנית אחת שנכשלה אצל חלק ניכר מהמגישות.</summary>
        StructuralRequirementFailed = 0,

        /// <summary>מקרה בדיקה אחד שנכשל אצל חלק ניכר מהמגישות.</summary>
        TestCaseFailed = 1,

        /// <summary>אף אחת לא עברה את התרגיל.</summary>
        NobodyPassed = 2,

        /// <summary>רוב המגישות לא הצליחו לקמפל בכלל.</summary>
        CompilationFailedForMost = 3
    }

    /// <summary>
    /// סיגנל אחד לפעמון ולדיג'סט היומי — <b>אגרגציה</b> על תרגיל אחד, לא הגשה בודדת.
    /// <para>
    /// ⚠️ אין ישות ואין טבלה. הרשומה הזו מחושבת על פי דרישה מתוך ההגשות בחלון תאריכים,
    /// ולכן היא אידמפוטנטית לפי התאריך ואין לה מצב "נקרא" בשרת. ר' plan-teacherNotifications.
    /// </para>
    /// <para>
    /// ⚠️ <b>מורה ומנהלת בלבד.</b> הרשומה חושפת כמה תלמידות נכשלו ובמה — לתלמידה אין בה
    /// שום ערך והיא לא אמורה לראות אותה. הפעמון של התלמידה נשאר על
    /// <c>GET /api/notifications/graded-submissions</c>.
    /// </para>
    /// </summary>
    public class ClassSignalDto
    {
        /// <summary>
        /// מזהה יציב לסיגנל, מורכב מסוג + תרגיל + הפרט. משמש את הלקוח ל-<c>track</c> ולסימון
        /// "נקרא" — אין Id של שורה, כי אין שורה.
        /// </summary>
        public string Key { get; set; } = "";

        public ClassSignalType Type { get; set; }

        public int LessonId { get; set; }
        public string LessonSubject { get; set; } = "";

        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = "";

        /// <summary>הפרט שהסיגנל מצביע עליו — נוסח הדרישה, או "בדיקה 3". ריק בסיגנלים על התרגיל כולו.</summary>
        public string? Detail { get; set; }

        /// <summary>כמה תלמידות נפגעו.</summary>
        public int AffectedCount { get; set; }

        /// <summary>מתוך כמה מגישות. המכנה הוא מי שהגישה בפועל, לא גודל הכיתה.</summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// המשפט בעברית כפי שהוא מוצג. מקור אחד לפעמון ולמייל בכוונה — שני ניסוחים לאותו
        /// סיגנל היו נקראים כשתי התראות שונות.
        /// </summary>
        public string Message { get; set; } = "";
    }
}
