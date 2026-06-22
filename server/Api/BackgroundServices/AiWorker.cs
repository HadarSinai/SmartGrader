//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using SmartGrader.Application.Services.BackgroundJobs;
//using SmartGrader.Application.Services.Feedback;
//using SmartGrader.Domain.Abstractions;
//using SmartGrader.Domain.Entities;

//namespace SmartGrader.Api.BackgroundServices
//{
//    public class AiWorker : BackgroundService
//    {
//        private readonly IAiJobQueue _queue;
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly ILogger<AiWorker> _logger;

//        public AiWorker(IAiJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<AiWorker> logger)
//        {
//            _queue = queue;
//            _scopeFactory = scopeFactory;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                var submissionId = await _queue.DequeueAsync(stoppingToken);

//                using var scope = _scopeFactory.CreateScope();
//                var submissions = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
//                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
//                var feedback = scope.ServiceProvider.GetRequiredService<IFeedbackService>();

//                var submission = await submissions.GetByIdAsync(submissionId, stoppingToken);
//                if (submission is null) continue;

//                // לא נוגעים אם כבר הסתיים
//                if (submission.Status is SubmissionStatus.Done or SubmissionStatus.AiFailed)
//                    continue;

//                try
//                {
//                    // PendingAi -> ProcessingAi
//                    if (submission.Status == SubmissionStatus.PendingAi)
//                    {
//                        submission.MarkProcessingAi();
//                        await uow.SaveChangesAsync(stoppingToken);
//                    }

//                    // בלי טסטים כרגע
//                    int passedTests = 0, totalTests = 0;

//                    // תיאור משימה - תבחרי מה שיש לך באמת ב-Assignment
//                    var assignmentDescription =
//                        submission.Assignment?.Title
//                        ?? "No assignment description";

//                    var aiJson = await feedback.GetFeedbackAsync(
//                        assignmentDescription,
//                        submission.SourceCode,
//                        passedTests,
//                        totalTests,
//                        stoppingToken);

//                    // נשמור את ה-JSON שה-AI החזיר בתור Comments
//                    // ניקוד זמני: 0 (בהמשך כשיהיו טסטים/ניקוד אמיתי)
//                    submission.MarkDone(0, aiJson);
//                    await uow.SaveChangesAsync(stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "AI Worker failed for submissionId={SubmissionId}", submissionId);

//                    // MarkAiFailed חוקי אצלך רק מ-ProcessingAi
//                    if (submission.Status == SubmissionStatus.ProcessingAi)
//                    {
//                        submission.MarkAiFailed(ex.Message);
//                        await uow.SaveChangesAsync(stoppingToken);
//                    }
//                }
//            }
//        }
//    }
//}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Api.BackgroundServices
{
    public class AiWorker : BackgroundService
    {
        private readonly IAiJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiWorker> _logger;

        public AiWorker(
            IAiJobQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<AiWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int submissionId = 0;

                try
                {
                    submissionId = await _queue.DequeueAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var submissions = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var feedback = scope.ServiceProvider.GetRequiredService<IFeedbackService>();

                    var submission = await submissions.GetByIdAsync(submissionId, stoppingToken);
                    if (submission is null)
                        continue;

                    // לא נוגעים אם כבר הסתיים
                    if (submission.Status is SubmissionStatus.Done or SubmissionStatus.AiFailed)
                        continue;

                    // PendingAi -> ProcessingAi (שומרים מיד)
                    if (submission.Status == SubmissionStatus.PendingAi)
                    {
                        submission.MarkProcessingAi();
                        await uow.SaveChangesAsync(stoppingToken);
                    }

                    // TODO: בהמשך להחליף ל-results אמיתיים מטסטים
                    int passedTests = 0, totalTests = 0;

                    var assignmentDescription =
                        (submission.Assignment?.Description ?? submission.Assignment?.Title)
                        ?? "No assignment description";

                    var aiJson = await feedback.GetFeedbackAsync(
                        assignmentDescription,
                        submission.SourceCode,
                        passedTests,
                        totalTests,
                        stoppingToken);

                    // אם השירות מחזיר מחרוזת של שגיאה - נחשב כ-AiFailed
                    if (IsAiError(aiJson))
                    {
                        submission.MarkAiFailed(aiJson);
                        await uow.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    submission.MarkDone(
                        score: totalTests > 0 ? (double)passedTests / totalTests * 100 : 0,
                        comments: aiJson);

                    await uow.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutdown רגיל
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI Worker failed for submissionId={SubmissionId}", submissionId);

                    // ננסה לסמן כ-AiFailed אם אפשר (ב-scope חדש)
                    try
                    {
                        using var scope2 = _scopeFactory.CreateScope();
                        var submissions2 = scope2.ServiceProvider.GetRequiredService<ISubmissionRepository>();
                        var uow2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        var submission2 = await submissions2.GetByIdAsync(submissionId, stoppingToken);
                        if (submission2 is not null && submission2.Status == SubmissionStatus.ProcessingAi)
                        {
                            submission2.MarkAiFailed(ex.Message);
                            await uow2.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception inner)
                    {
                        _logger.LogError(inner, "Failed to mark AiFailed for submissionId={SubmissionId}", submissionId);
                    }
                }
            }
        }

        private static bool IsAiError(string? aiJson)
        {
            if (string.IsNullOrWhiteSpace(aiJson)) return true;

            // לפי מה שכבר ראית אצלך
            if (aiJson.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase)) return true;
            if (aiJson.Contains("You exceeded your current quota", StringComparison.OrdinalIgnoreCase)) return true;
            if (aiJson.StartsWith("שגיאת AI", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}
