using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Entities;

public interface IFeedbackService
{
    Task<AiFeedbackResult> GetFeedbackAsync(
        string assignmentDescription,
        string sourceCode,
        int passedTests,
        int totalTests,
        IReadOnlyList<TestCaseResult> testDetails,
        CancellationToken ct);
}
