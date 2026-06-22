using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
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

        public UpdateSubmissionHandler(
            ISubmissionRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

            // 🎯 מעדכנים מה־DTO
            _mapper.Map(request.Dto, submission);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 🎯 החזרה ב־DTO
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
