namespace SmartGrader.Infrastructure.Services.CodeRunner;

public sealed class Judge0Options
{
    public string BaseUrl { get; init; } = "http://localhost:2358";
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
    public int LanguageId { get; init; } = 51; // 51 = C# (Mono)
}
