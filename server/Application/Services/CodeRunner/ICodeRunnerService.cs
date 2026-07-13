using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.CodeRunner;

public interface ICodeRunnerService
{
    Task<RunnerResult> RunAsync(
        string sourceCode,
        string methodName,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default);
}
