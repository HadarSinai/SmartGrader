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

        // teacherId: null — קורא מערכת, לא משתמש/ת. הבדיקה רצה על כל הגשה בתור בלי קשר לבעלות.
        var submission = await _submissions.GetByIdAsync(submissionId, teacherId: null, ct);

        // ההגשה נעלמה מה-DB בין הכנסת העבודה לתור לבין הרצתה. מאז שהמחיקות הוגבלו
        // (Restrict + חסימה ב-handlers) זה כמעט בלתי אפשרי — הגשה ב-PendingAi/ProcessingAi
        // לא ניתנת למחיקה, וגם מחיקת שיעור/תרגיל/תלמידה נחסמת כשיש עבודה. נשאר כהגנה.
        if (submission is null) return;

        if (submission.Status is SubmissionStatus.Done
            or SubmissionStatus.AiFailed
            or SubmissionStatus.CompilationFailed
            or SubmissionStatus.JudgeUnavailable)
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
            RunnerResult runnerResult;
            try
            {
                runnerResult = assignment.GradingMode switch
                {
                    // תוכנית שלמה עם Main של התלמיד — בלי עטיפה. אם הוגשה כקובץ יחיד (בלי
                    // Files), עוטפים את SourceCode כ-SubmissionFile יחיד לאותו נתיב מיזוג.
                    GradingMode.FullProgram => await _codeRunner.RunProgramAsync(
                        submission.SourceFiles.Count > 0
                            ? submission.SourceFiles
                            : new List<SubmissionFile> { new() { FileName = "Program.cs", Content = submission.SourceCode ?? "" } },
                        assignment.Tests,
                        ct),

                    GradingMode.MultiFileMethod => await _codeRunner.RunAsync(
                        submission.SourceFiles,
                        assignment.ExpectedFiles,
                        assignment.Tests,
                        ct),

                    // GradingMode.Method וכל ערך אחר (הגנה עתידית)
                    _ => await _codeRunner.RunAsync(
                        submission.SourceCode,
                        assignment.MethodName,
                        assignment.Tests,
                        ct),
                };
            }
            catch (Exception ex) when (ex is HttpRequestException
                or JsonException
                or TaskCanceledException
                or CodeRunnerUnavailableException)
            {
                // כשל תשתית: Judge0 לא זמין / timeout / תשובה לא תקינה — לא כשל של קוד התלמיד ולא של ה-AI.
                // לפי ההחלטה: אין retry אוטומטי — רק סימון ברור ולוג ייעודי.
                _logger.LogError(ex, "Judge0 unavailable for submissionId={SubmissionId}", submissionId);

                submission.MarkJudgeUnavailable(ex.Message);
                await _uow.SaveChangesAsync(ct);

                await _logWriter.WriteAsync(
                    LogActionTypes.JudgeUnavailable,
                    $"מערכת בדיקת הקוד אינה זמינה עבור הגשה #{submissionId}: {ex.Message}",
                    LogStatuses.Error,
                    LogSystemSources.AiWorker,
                    lessonId: submission.Assignment?.LessonId,
                    assignmentId: submission.AssignmentId,
                    ct: ct);
                return;
            }

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

            // בהגשות רב-קובציות (FullProgram או MultiFileMethod) SourceCode ריק —
            // מעבירים ל-AI את תוכן הקבצים המאוחד במקום מחרוזת ריקה.
            var codeForFeedback = !string.IsNullOrWhiteSpace(submission.SourceCode)
                ? submission.SourceCode
                : string.Join("\n\n", submission.SourceFiles.Select(f => f.Content));

            var aiFeedback = await _feedback.GetFeedbackAsync(
                assignmentDescription,
                codeForFeedback,
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
