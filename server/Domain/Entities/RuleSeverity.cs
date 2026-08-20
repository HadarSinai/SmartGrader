using System.Text.Json.Serialization;

namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// חומרת הדרישה — מה קורה כשהיא לא מתקיימת.
    /// <para>
    /// ההבחנה בין <see cref="Blocking"/> ל-<see cref="Scored"/> היא לב התכנון: המורה ניסחה
    /// את זה כ"אם התרגיל דרש רקורסיה והיא כתבה לולאות — זה כאילו לא עשתה בכלל". זו
    /// <i>דחייה</i>, לא ציון נמוך.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RuleSeverity
    {
        /// <summary>
        /// 🔴 חוסמת — אין ציון כלל, ההגשה חוזרת לתלמידה. הדרישה היא שער ולכן אינה נושאת נקודות.
        /// </summary>
        Blocking = 0,

        /// <summary>
        /// 🟡 מנוקדת — אי-עמידה מפסידה את <see cref="StructuralRule.Points"/> הנקודות שלה,
        /// בשלמותן. דרישה היא תנאי, לא מדידה: אין ניקוד חלקי על "לכל היותר 3 if" כשנכתבו 4.
        /// </summary>
        Scored = 1,

        /// <summary>⚪ המלצה — הערה במשוב בלבד, בלי שום השפעה על הציון.</summary>
        Advisory = 2
    }
}
