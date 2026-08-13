using System.Text.Json.Serialization;

namespace SmartGrader.Application.Services.Feedback;

// הערה: זהו שרטוט ראשוני של מבנה המשוב, לא סופי — המורה עשויה לשנות/להוסיף/להסיר
// קטגוריות בעתיד. הריכוז של השינוי ברשומה הזו + ה-DTO המקביל + הקליינט שומר על
// עדכון עתידי קומפקטי ומרוכז במקום אחד.
public sealed record AiFeedbackResult(
    [property: JsonPropertyName("good")] List<string>? Good,
    [property: JsonPropertyName("issues")] AiFeedbackIssues? Issues,
    [property: JsonPropertyName("minimal_changes")] List<string>? MinimalChanges,
    [property: JsonPropertyName("optional_full_solution")] string? OptionalFullSolution,
    [property: JsonPropertyName("scores")] AiFeedbackScores? Scores)
{
    // true אם המשוב פורש בהצלחה למבנה ה-JSON הצפוי. כאשר false, RawResponse
    // מכיל את הטקסט המקורי הלא-מפורש (או שדה Comments הישן שהועתק במיגרציה)
    // כדי שהקליינט תמיד יהיה לו מה להציג גם כשהפירוש נכשל.
    [property: JsonIgnore]
    public bool ParseSucceeded { get; init; } = true;

    [property: JsonIgnore]
    public string? RawResponse { get; init; }
}

public sealed record AiFeedbackIssues(
    [property: JsonPropertyName("correctness")] List<string>? Correctness,
    [property: JsonPropertyName("readability")] List<string>? Readability,
    [property: JsonPropertyName("performance")] List<string>? Performance
);

public sealed record AiFeedbackScores(
    [property: JsonPropertyName("test_score")] double? TestScore,
    [property: JsonPropertyName("code_quality_score")] double? CodeQualityScore,
    [property: JsonPropertyName("efficiency_score")] double? EfficiencyScore,
    [property: JsonPropertyName("final_score")] double? FinalScore
);
