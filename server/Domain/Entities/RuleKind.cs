using System.Text.Json.Serialization;

namespace SmartGrader.Domain.Entities
{
    /// <summary>
    /// סוג הדרישה המבנית — מה בדיוק נבדק על הקוד של התלמידה.
    /// <para>
    /// ⚠️ מסודר כמחרוזת ב-JSON (<see cref="JsonStringEnumConverter"/>) ולא כמספר, מפני
    /// ש-<see cref="StructuralRule"/> נשמר כ-<c>StructuralRulesJson</c> על התרגיל. קטלוג
    /// הדרישות גדל כל סמסטר, ושמירה כמספר הופכת כל הוספה באמצע לשינוי משמעות של כל
    /// התרגילים הקיימים בבסיס הנתונים.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RuleKind
    {
        /// <summary>חובה שהמבנה יופיע לפחות פעם אחת.</summary>
        MustUse = 0,

        /// <summary>אסור שהמבנה יופיע כלל.</summary>
        MustNotUse = 1,

        /// <summary>לפחות <see cref="StructuralRule.Threshold"/> מופעים.</summary>
        AtLeast = 2,

        /// <summary>לכל היותר <see cref="StructuralRule.Threshold"/> מופעים.</summary>
        AtMost = 3
    }
}
