using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.CodeRunner;

public interface ICodeRunnerService
{
    // נתיב קובץ יחיד (legacy) — ללא שינוי התנהגות. sourceCode הוא גוף מתודה בודד,
    // TestCase.Input הוא מחרוזת stdin חד-שורתית מופרדת ברווחים.
    Task<RunnerResult> RunAsync(
        string sourceCode,
        string methodName,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default);

    // נתיב רב-קובצי: sourceFiles מורכבים לכדי יחידת קומפילציה אחת, ו-TestCase.Input
    // הוא מערך ארגומנטים בקידוד JSON (למשל "[1, 2]") ולא stdin מופרד ברווחים.
    Task<RunnerResult> RunAsync(
        IReadOnlyList<SubmissionFile> sourceFiles,
        IReadOnlyList<ExpectedFile> expectedFiles,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default);
}
