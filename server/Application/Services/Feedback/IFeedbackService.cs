public interface IFeedbackService
{
    Task<string> GetFeedbackAsync(
        string assignmentDescription,
        string sourceCode,
        int passedTests,
        int totalTests
        , CancellationToken ct);
}
