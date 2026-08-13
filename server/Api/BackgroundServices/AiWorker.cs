using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartGrader.Application.Common.Interfaces;
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
    private readonly ILogWriter _logWriter;
    private readonly ILogger<AiWorker> _logger;

    public AiWorker(
        ISubmissionRepository submissions,
        IUnitOfWork uow,
        IFeedbackService feedback,
        ICodeRunnerService codeRunner,
        ILogWriter logWriter,
        ILogger<AiWorker> logger)
    {
        _submissions = submissions;
        _uow = uow;
        _feedback = feedback;
        _codeRunner = codeRunner;
        _logWriter = logWriter;
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

            await _logWriter.WriteAsync(
                LogActionTypes.AiGradingStarted,
                $"החלה בדיקת הגשה #{submissionId}",
                LogStatuses.Success,
                LogSystemSources.AiWorker,
                lessonId: submission.Assignment?.LessonId,
                assignmentId: submission.AssignmentId,
                ct: ct);

            var assignment = submission.Assignment!;
            var runnerResult = assignment.ExpectedFiles.Count > 0
                ? await _codeRunner.RunAsync(
                    submission.SourceFiles,
                    assignment.ExpectedFiles,
                    assignment.Tests,
                    ct)
                : await _codeRunner.RunAsync(
                    submission.SourceCode,
                    assignment.MethodName,
                    assignment.Tests,
                    ct);

            // גם בנתיב כשל קומפילציה נשמר (הרשימה תהיה ריקה) — לעקביות עם שאר הנתיב
            submission.SetTestResults(runnerResult.Details.ToList());

            if (runnerResult.HasCompileError)
            {
                submission.MarkCompilationFailed(runnerResult.CompileError ?? "Unknown compile error");
                await _uow.SaveChangesAsync(ct);

                await _logWriter.WriteAsync(
                    LogActionTypes.CompilationFailed,
                    $"שגיאת קומפילציה בהגשה #{submissionId}: {runnerResult.CompileError ?? "Unknown compile error"}",
                    LogStatuses.Error,
                    LogSystemSources.AiWorker,
                    lessonId: submission.Assignment?.LessonId,
                    assignmentId: submission.AssignmentId,
                    ct: ct);
                return;
            }

            var assignmentDescription =
                (submission.Assignment?.Description ?? submission.Assignment?.Title)
                ?? "No assignment description";

            var aiFeedback = await _feedback.GetFeedbackAsync(
                assignmentDescription,
                submission.SourceCode,
                runnerResult.Passed,
                runnerResult.Total,
                runnerResult.Details,
                ct);

            // הציון עדיין מחושב מתוצאות הטסטים (לא הציון העצמי של ה-AI) — זהו פער ידוע
            // שמוזכר כאן במפורש כנקודת החלטה עתידית, לא כשינוי בשלב הנוכחי.
            submission.MarkDone(
                score: runnerResult.Total > 0 ? (double)runnerResult.Passed / runnerResult.Total * 100 : 0,
                feedbackJson: JsonSerializer.Serialize(aiFeedback));

            await _uow.SaveChangesAsync(ct);

            await _logWriter.WriteAsync(
                LogActionTypes.AiGradingCompleted,
                $"בדיקת הגשה #{submissionId} הושלמה. ציון: {submission.Score:0.#}",
                LogStatuses.Success,
                LogSystemSources.AiWorker,
                lessonId: submission.Assignment?.LessonId,
                assignmentId: submission.AssignmentId,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Worker failed for submissionId={SubmissionId}", submissionId);

            if (submission.Status == SubmissionStatus.ProcessingAi)
            {
                submission.MarkAiFailed(ex.Message);
                await _uow.SaveChangesAsync(ct);
            }

            await _logWriter.WriteAsync(
                LogActionTypes.AiFailed,
                $"כשל בבדיקת הגשה #{submissionId}: {ex.Message}",
                LogStatuses.Error,
                LogSystemSources.AiWorker,
                lessonId: submission.Assignment?.LessonId,
                assignmentId: submission.AssignmentId,
                ct: ct);
        }
    }
}
