using AutoMapper;
using Hangfire;
using MediatR;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBackgroundJobClient _jobClient;

        public UpdateSubmissionHandler(
            ISubmissionRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IBackgroundJobClient jobClient)
        {
            _repository = repository;
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

            // 🎯 עריכה מותרת רק להגשה שנכשלה — לא להגשה שנבדקה או שנמצאת בבדיקה
            if (submission.Status is not (SubmissionStatus.CompilationFailed
                or SubmissionStatus.JudgeUnavailable
                or SubmissionStatus.AiFailed))
                throw new BusinessRuleException(
                    "לא ניתן לערוך הגשה זו — עריכה אפשרית רק להגשה שנכשלה (שגיאת קומפילציה, תקלת מערכת או שגיאת בדיקה)");

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
            submission.MarkPendingAi();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));

            // 🎯 החזרה ב־DTO
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
