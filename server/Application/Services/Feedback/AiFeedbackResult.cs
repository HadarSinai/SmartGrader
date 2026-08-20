using System.Text.Json.Serialization;

namespace SmartGrader.Application.Services.Feedback;

/// <summary>
/// המשוב המילולי בעברית. זהו כל מה שהמודל תורם.
/// <para>
/// 🔴 <b>אין כאן מספרים, ולא במקרה.</b> את הציון קובעים Roslyn (הדרישות), מריץ הקוד
/// (הטסטים) ו-<c>ScoreCalculator</c> (הרובריקה) — כולם דטרמיניסטיים, כך שאותו קוד מקבל
/// תמיד אותו ציון. שדה <c>scores</c> שהיה כאן ביקש מהמודל לנקד בעצמו, וכשהתבנית בפרומפט
/// הכילה אפסים ממשיים הוא העתיק אותם כלשונם — כל תלמידה קיבלה איכות קוד 0 ויעילות 0.
/// </para>
/// </summary>
public sealed record AiFeedbackResult(
    [property: JsonPropertyName("good")] List<string>? Good,
    [property: JsonPropertyName("issues")] AiFeedbackIssues? Issues,
    [property: JsonPropertyName("minimal_changes")] List<string>? MinimalChanges)
{
    // true אם המשוב פורש בהצלחה למבנה ה-JSON הצפוי. כאשר false, RawResponse
    // מכיל את הטקסט המקורי הלא-מפורש כדי שלקליינט תמיד יהיה מה להציג.
    [property: JsonIgnore]
    public bool ParseSucceeded { get; init; } = true;

    [property: JsonIgnore]
    public string? RawResponse { get; init; }

    /// <summary>
    /// משוב שאינו מהמודל אלא מהעובדות שכבר נקבעו — לשימוש כשהמודל אינו זמין.
    /// <para>
    /// ⚠️ זהו ההבדל בין "הגשה בלי ציון ובלי הסבר" לבין "הגשה עם ההסבר הדטרמיניסטי".
    /// Roslyn או הטסטים כבר קבעו את העובדה; תקלה ב-OpenAI רשאית לפגוע רק בניסוח.
    /// </para>
    /// </summary>
    public static AiFeedbackResult Deterministic(IEnumerable<string> findings) =>
        new(Good: null, Issues: null, MinimalChanges: null)
        {
            ParseSucceeded = false,
            RawResponse = string.Join("\n", findings.Append("(ההסבר המפורט אינו זמין כרגע)"))
        };
}

public sealed record AiFeedbackIssues(
    [property: JsonPropertyName("correctness")] List<string>? Correctness,
    [property: JsonPropertyName("readability")] List<string>? Readability,
    [property: JsonPropertyName("performance")] List<string>? Performance
);
