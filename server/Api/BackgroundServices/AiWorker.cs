using Microsoft.Extensions.Logging;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Api.BackgroundServices;

public class AiWorker : IGradeSubmissionJob
{
    private readonly ISubmissionRepository _submissions;
    private readonly IUnitOfWork _uow;
    private readonly IFeedbackService _feedback;
    private readonly ICodeRunnerService _codeRunner;
    private readonly ILogger<AiWorker> _logger;

    public AiWorker(
        ISubmissionRepository submissions,
        IUnitOfWork uow,
        IFeedbackService feedback,
        ICodeRunnerService codeRunner,
        ILogger<AiWorker> logger)
    {
        _submissions = submissions;
        _uow = uow;
        _feedback = feedback;
        _codeRunner = codeRunner;
        _logger = logger;
    }

    public async Task ExecuteAsync(int submissionId)
    {
        var ct = CancellationToken.None;

        var submission = await _submissions.GetByIdAsync(submissionId, ct);
        if (submission is null) return;

        if (submission.Status is SubmissionStatus.Done or SubmissionStatus.AiFailed)
            return;

        try
        {
            if (submission.Status == SubmissionStatus.PendingAi)
            {
                submission.MarkProcessingAi();
                await _uow.SaveChangesAsync(ct);
            }

            var runnerResult = await _codeRunner.RunAsync(
                submission.SourceCode,
                submission.Assignment!.MethodName,
                submission.Assignment.Tests,
                ct);

            if (runnerResult.HasCompileError)
            {
                submission.MarkCompilationFailed(runnerResult.CompileError ?? "Unknown compile error");
                await _uow.SaveChangesAsync(ct);
                return;
            }

            var assignmentDescription =
                (submission.Assignment?.Description ?? submission.Assignment?.Title)
                ?? "No assignment description";

            var aiJson = await _feedback.GetFeedbackAsync(
                assignmentDescription,
                submission.SourceCode,
                runnerResult.Passed,
                runnerResult.Total,
                ct);

            submission.MarkDone(
                score: runnerResult.Total > 0 ? (double)runnerResult.Passed / runnerResult.Total * 100 : 0,
                comments: aiJson);

            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Worker failed for submissionId={SubmissionId}", submissionId);

            if (submission.Status == SubmissionStatus.ProcessingAi)
            {
                submission.MarkAiFailed(ex.Message);
                await _uow.SaveChangesAsync(ct);
            }
        }
    }
}
