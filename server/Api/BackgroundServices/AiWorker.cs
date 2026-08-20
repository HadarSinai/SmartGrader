using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using SmartGrader.Domain.Services;

namespace SmartGrader.Api.BackgroundServices;

/// <summary>
/// צנרת הבדיקה של הגשה, בסדר הזה בדיוק:
/// <code>
/// 1. Roslyn בודק את הדרישות          ← מיידי · מקומי · ללא עלות
///    ├─ דרישה חוסמת נכשלה?
///    │    → RequirementsNotMet · אין ציון · ה-AI מסביר · הגשה חוזרת
///    │    → עצירה. Judge0 אינו נקרא בכלל.
///    └─ נקי?
/// 2.      Judge0 מריץ את הטסטים
/// 3.      ScoreCalculator מחשב את הציון
/// 4.      ה-AI כותב את המשוב המילולי
/// </code>
/// <para>
/// בדיקת הדרישות לפני ההרצה אינה רק סדר הגיוני: הגשה שלא עומדת בדרישה חוסמת אינה צורכת
/// מכסת הרצה בכלל.
/// </para>
/// <para>
/// 🔴 <b>מודל שפה אינו קובע כאן שום מספר.</b> Roslyn מכריע אם דרישה התקיימה, מריץ הקוד
/// מכריע אם הפלט תאם, ו-<see cref="ScoreCalculator"/> מכריע את הציון. לכן גם: <b>כשל של
/// ה-AI לעולם אינו מוחק ציון שכבר חושב</b> — הוא רשאי לפגוע רק בניסוח.
/// </para>
/// </summary>
public class AiWorker : IGradeSubmissionJob
{
    private readonly ISubmissionRepository _submissions;
    private readonly IUnitOfWork _uow;
    private readonly IFeedbackService _feedback;
    private readonly ICodeRunnerService _codeRunner;
    private readonly ICodeAnalysisService _codeAnalysis;
    private readonly ILogWriter _logWriter;
    private readonly ILogger<AiWorker> _logger;

    public AiWorker(
        ISubmissionRepository submissions,
        IUnitOfWork uow,
        IFeedbackService feedback,
        ICodeRunnerService codeRunner,
        ICodeAnalysisService codeAnalysis,
        ILogWriter logWriter,
        ILogger<AiWorker> logger)
    {
        _submissions = submissions;
        _uow = uow;
        _feedback = feedback;
        _codeRunner = codeRunner;
        _codeAnalysis = codeAnalysis;
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
            or SubmissionStatus.JudgeUnavailable
            or SubmissionStatus.RequirementsNotMet)
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
            var code = CodeOf(submission);
            var description = (assignment.Description ?? assignment.Title) ?? "No assignment description";

            // ── 1. הדרישות המבניות ────────────────────────────────────────────
            var analysis = _codeAnalysis.Analyze(code, assignment.StructuralRules);
            submission.SetStructuralResults(analysis.Results.ToList());

            var failedBlocking = analysis.FailedBlockingRules;

            // ⚠️ קוד שלא נפרש אינו מפעיל את השער. בלי התנאי הזה נקודה-פסיק חסרה נספרת כ"אין
            // רקורסיה", והתלמידה מקבלת "התרגיל דרש רקורסיה" במקום שגיאת הקומפילציה האמיתית.
            // במקרה כזה ההגשה ממשיכה ל-Judge0 ומקבלת את השגיאה שבאמת קרתה.
            if (failedBlocking.Count > 0 && !analysis.HasSyntaxErrors)
            {
                await HandleRequirementsNotMetAsync(submission, description, code, failedBlocking, ct);
                return;
            }

            // ── 2. הרצת הטסטים ────────────────────────────────────────────────
            RunnerResult runnerResult;
            try
            {
                // ⚠️ הבחירה בין המסלולים חייבת להישאר משותפת עם אימות מקרי הבדיקה של המורה
                // (VerifyTestCasesHandler) — ר' GradingModeRunner. אימות שרץ במסלול אחר מהניקוד
                // מבטיח למורה משהו שלא נכון.
                runnerResult = await GradingModeRunner.RunAsync(
                    _codeRunner,
                    assignment.GradingMode,
                    submission.SourceFiles,
                    submission.SourceCode,
                    assignment.MethodName,
                    assignment.ExpectedFiles,
                    assignment.Tests,
                    ct);
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
                await HandleCompilationFailedAsync(submission, description, code, runnerResult, ct);
                return;
            }

            // ── 3. הציון ──────────────────────────────────────────────────────
            // הנוסחה ישבה כאן כביטוי בשורה אחת (passed/total*100) והיא עברה ל-ScoreCalculator:
            // שער מקרי הליבה, הרובריקה ומקרה "הכול שערים" הם כללים, לא ביטוי.
            var breakdown = ScoreCalculator.Calculate(
                assignment.TestsAllocation,
                runnerResult.Details,
                analysis.Results,
                assignment.MaxScore);

            // ── 4. המשוב המילולי ──────────────────────────────────────────────
            // 🔴 הציון כבר חושב, ולכן הקריאה הזו עטופה: קודם היה כאן catch חיצוני שסימן
            // AiFailed, וציון תקין לחלוטין הלך לאיבוד בגלל תקלה ב-OpenAI.
            var aiFeedback = await SafeFeedbackAsync(
                () => _feedback.GetGradingFeedbackAsync(
                    description,
                    code,
                    runnerResult.Passed,
                    runnerResult.Total,
                    runnerResult.Details,
                    analysis.Results,
                    ct),
                submissionId);

            submission.MarkDone(breakdown, JsonSerializer.Serialize(aiFeedback));
            await _uow.SaveChangesAsync(ct);

            await _logWriter.WriteAsync(
                LogActionTypes.AiGradingCompleted,
                $"בדיקת הגשה #{submissionId} הושלמה. ציון: {breakdown.Total:0.#} " +
                $"(בדיקות {breakdown.TestPoints:0.#} · דרישות {breakdown.RulePoints:0.#})",
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

    // ── מסלולי הכשל ───────────────────────────────────────────────────────────

    /// <summary>
    /// דרישה חוסמת לא התקיימה: אין ציון, Judge0 לא נקרא, וההגשה נשארת פתוחה.
    /// <para>
    /// ⚠️ <b>הסטטוס והממצאים נשמרים לפני הפנייה ל-AI.</b> Roslyn כבר קבע את העובדה, ותקלה
    /// ב-OpenAI אסור שתשאיר את התלמידה עם סטטוס חשוף בלי שום הסבר.
    /// </para>
    /// </summary>
    private async Task HandleRequirementsNotMetAsync(
        Submission submission,
        string description,
        string code,
        IReadOnlyList<StructuralRuleResult> failedBlocking,
        CancellationToken ct)
    {
        submission.MarkRequirementsNotMet();
        await _uow.SaveChangesAsync(ct);

        var aiFeedback = await SafeFeedbackAsync(
            () => _feedback.GetRequirementFeedbackAsync(description, code, failedBlocking, ct),
            submission.Id,
            () => AiFeedbackResult.Deterministic(
                failedBlocking.Select(StructuralRuleDescriber.DescribeFailure)));

        submission.SetFeedback(JsonSerializer.Serialize(aiFeedback));
        await _uow.SaveChangesAsync(ct);

        await _logWriter.WriteAsync(
            LogActionTypes.AiGradingCompleted,
            $"הגשה #{submission.Id} לא עמדה בדרישות חוסמות: " +
            string.Join(" · ", failedBlocking.Select(r => StructuralRuleDescriber.Describe(r.Rule))),
            LogStatuses.Success,
            LogSystemSources.AiWorker,
            lessonId: submission.Assignment?.LessonId,
            assignmentId: submission.AssignmentId,
            ct: ct);
    }

    /// <summary>
    /// שגיאת קומפילציה. בעבר היה כאן <c>return</c> מוקדם והתלמידה קיבלה
    /// <c>error CS0103</c> גולמי; עכשיו ה-AI מתרגם אותו לעברית — וזה בדיוק המקום שבו
    /// הוא תורם הכי הרבה, ההבדל בין תלמידה תקועה לתלמידה שממשיכה.
    /// </summary>
    private async Task HandleCompilationFailedAsync(
        Submission submission,
        string description,
        string code,
        RunnerResult runnerResult,
        CancellationToken ct)
    {
        var compileError = runnerResult.CompileError ?? "Unknown compile error";

        submission.MarkCompilationFailed(compileError);
        await _uow.SaveChangesAsync(ct);

        var aiFeedback = await SafeFeedbackAsync(
            () => _feedback.GetCompileErrorFeedbackAsync(description, code, compileError, ct),
            submission.Id,
            () => AiFeedbackResult.Deterministic(new[] { "❌ הקוד לא עבר הידור.", compileError }));

        submission.SetFeedback(JsonSerializer.Serialize(aiFeedback));
        await _uow.SaveChangesAsync(ct);

        await _logWriter.WriteAsync(
            LogActionTypes.CompilationFailed,
            $"שגיאת קומפילציה בהגשה #{submission.Id}: {compileError}",
            LogStatuses.Error,
            LogSystemSources.AiWorker,
            lessonId: submission.Assignment?.LessonId,
            assignmentId: submission.AssignmentId,
            ct: ct);
    }

    // ── עזרים ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// קורא ל-AI בלי לתת לו להפיל את הבדיקה.
    /// <para>
    /// 🔴 זה ההבדל בין "ה-AI נפל ולכן אין ציון" לבין "ה-AI נפל ולכן אין הסבר מפורט".
    /// המימוש ב-<c>OpenAiFeedbackService</c> כבר חוזר לגיבוי בכל כשל צפוי; העטיפה כאן היא
    /// הרשת האחרונה, לכשל שלא נצפה.
    /// </para>
    /// </summary>
    private async Task<AiFeedbackResult> SafeFeedbackAsync(
        Func<Task<AiFeedbackResult>> call,
        int submissionId,
        Func<AiFeedbackResult>? fallback = null)
    {
        try
        {
            return await call();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Feedback generation failed for submissionId={SubmissionId}", submissionId);

            return fallback?.Invoke()
                ?? AiFeedbackResult.Deterministic(new[] { "הבדיקה הושלמה." });
        }
    }

    /// <summary>
    /// הקוד לניתוח ולמשוב. בהגשות רב-קובציות <c>SourceCode</c> ריק, והקבצים מחוברים —
    /// אותה בחירה כמו ב-<c>GradingModeRunner.JoinFiles</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ מספרי השורות שהמנתח מדווח מתייחסים לטקסט המחובר. בהגשה של קובץ יחיד — המקרה
    /// הרווח כאן — הם זהים לשורות בקובץ.
    /// </remarks>
    private static string CodeOf(Submission submission) =>
        !string.IsNullOrWhiteSpace(submission.SourceCode)
            ? submission.SourceCode
            : string.Join("\n\n", submission.SourceFiles.Select(f => f.Content));
}
