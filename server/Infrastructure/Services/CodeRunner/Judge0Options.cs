namespace SmartGrader.Infrastructure.Services.CodeRunner;

public sealed class Judge0Options
{
    public string BaseUrl { get; init; } = "http://localhost:2358";
    public string? ApiKey { get; init; }

    /// <summary>מגבלת cpu_time_limit שנשלחת ל-Judge0 עצמו (זמן ריצה של קוד התלמיד).</summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Timeout של ה-HttpClient מול Judge0 (זיהוי מהיר של שירות שלא עונה).
    /// חייב להיות גדול מ-TimeoutSeconds כדי לא לקטוע הרצה תקינה.
    /// </summary>
    public int HttpTimeoutSeconds { get; init; } = 30;

    public int LanguageId { get; init; } = 51; // 51 = C# (Mono)
}
