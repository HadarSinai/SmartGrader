namespace SmartGrader.Application.Services.CodeRunner;

public sealed record RunnerResult(
    int Passed,
    int Total,
    bool HasCompileError,
    string? CompileError,
    IReadOnlyList<TestCaseResult> Details
);
