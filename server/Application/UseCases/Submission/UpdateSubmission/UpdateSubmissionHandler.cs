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
            // 🎯 שולפים ההגשה לפי SubmissionId
            var submission = await _repository.GetByIdAsync(
                request.SubmissionId,
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

            // 🎯 עדכון הקוד, איפוס הסטטוס ל-PendingAi ותור בדיקה מחדש
            submission.UpdateSourceCode(request.Dto.SourceCode);
            submission.MarkPendingAi();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));

            // 🎯 החזרה ב־DTO
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
