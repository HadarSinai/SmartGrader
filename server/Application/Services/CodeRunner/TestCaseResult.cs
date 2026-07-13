namespace SmartGrader.Application.Services.CodeRunner;

public sealed record TestCaseResult(
    string Input,
    string Expected,
    string Actual,
    bool Passed,
    string? Error
);
