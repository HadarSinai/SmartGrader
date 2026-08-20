using AutoMapper;
using Hangfire;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public class UpdateSubmissionHandler
        : IRequestHandler<UpdateSubmissionCommand, SubmissionResponseDto>
    {
        private readonly ISubmissionRepository _repository;
        private readonly ILessonResultRepository _lessonResults;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBackgroundJobClient _jobClient;

        public UpdateSubmissionHandler(
            ISubmissionRepository repository,
            ILessonResultRepository lessonResults,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IBackgroundJobClient jobClient)
        {
            _repository = repository;
            _lessonResults = lessonResults;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobClient = jobClient;
        }

        public async Task<SubmissionResponseDto> Handle(
            UpdateSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            // 🎯 שולפים ההגשה לפי SubmissionId — כבר מסונן לפי בעלות המורה על השיעור
            var submission = await _repository.GetByIdAsync(
                request.SubmissionId,
                request.TeacherId,
                cancellationToken);

            if (submission is null)
                throw new NotFoundException(nameof(Submission), request.SubmissionId);

            // 🎯 בדיקה שההגשה שייכת לסטודנט הנכון
            if (submission.StudentId != request.StudentId)
                throw new NotFoundException(
                    "Submission does not belong to this student.",
                    request.SubmissionId);

            var retryThreshold = submission.Assignment?.RetryThreshold
                                 ?? Assignment.DefaultRetryThreshold;

            // 🎯 מי רשאית להגיש שוב: כשל (קומפילציה / AI / תקלת מערכת / דרישה חוסמת) פתוח
            // תמיד, והגשה שנבדקה בהצלחה פתוחה כל עוד הציון מתחת לסף — בלי הגבלת ניסיונות.
            // ⚠️ הכלל עצמו חי ב-Submission.CanResubmit ונאכף שוב ב-MarkPendingAi. בדיקה
            // שיושבת רק כאן נעקפת בשקט על ידי כל קורא חדש.
            if (!submission.CanResubmit(retryThreshold))
                throw new BusinessRuleException(
                    submission.Status == SubmissionStatus.Done
                        ? $"לא ניתן להגיש שוב — הציון {submission.Score:0.#} עומד בסף {retryThreshold}. " +
                          "המורה יכולה לאשר הגשה נוספת."
                        : "לא ניתן לערוך הגשה זו — היא נמצאת כרגע בבדיקה.");

            // 🎯 נעילה גוברת גם על אישור המורה: שיעור שסוכם לתלמידה או כיתה בארכיון
            var isLocked = await SubmissionLock.IsLockedAsync(_lessonResults, submission, cancellationToken);
            if (isLocked)
                throw new BusinessRuleException(SubmissionLock.Message);

            // 🎯 הגבלת קצב: בלי תקרת ניסיונות, while(true){} עולה cpu_time_limit × מספר
            // הטסטים שניות וניתן לשלוח אותו שוב מיד. בולע גם לחיצה כפולה על "שליחה".
            if (submission.IsRateLimited(DateTime.UtcNow))
                throw new BusinessRuleException(
                    $"נא להמתין {Submission.MinResubmitInterval.TotalMinutes:0} דקות בין הגשה להגשה.");

            // 🎯 הגשה חוזרת לתרגיל רב-קובצי חייבת לכלול את כל הקבצים הצפויים — אותה בדיקה
            // כמו ב-CreateSubmissionHandler, אחרת הגשה חוזרת חלקית יוצרת הגשה שאי אפשר לבדוק
            var expectedFiles = submission.Assignment?.ExpectedFiles ?? new();
            if (expectedFiles.Count > 0)
            {
                var submittedNames = (request.Dto.Files ?? new())
                    .Select(f => f.FileName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = expectedFiles
                    .Select(f => f.FileName)
                    .Where(name => !submittedNames.Contains(name))
                    .ToList();

                if (missing.Count > 0)
                    throw new BusinessRuleException(
                        $"חסרים קבצים בהגשה: {string.Join(", ", missing)}");
            }

            // 🎯 עדכון הקוד, איפוס הסטטוס ל-PendingAi ותור בדיקה מחדש
            var sourceFiles = request.Dto.Files?
                .Select(f => new SubmissionFile { FileName = f.FileName, Content = f.Content })
                .ToList();

            submission.UpdateSourceCode(request.Dto.SourceCode, sourceFiles);

            // מארכב את הניסיון הקודם ומאפס. הכלל נאכף כאן שוב, בדומיין — ר' MarkPendingAi.
            submission.MarkPendingAi(retryThreshold, isLocked);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));

            // 🎯 החזרה ב־DTO
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
