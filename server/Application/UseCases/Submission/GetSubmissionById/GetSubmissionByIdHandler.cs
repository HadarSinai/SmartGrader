using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    public class GetSubmissionByIdHandler
        : IRequestHandler<GetSubmissionByIdQuery, SubmissionResponseDto>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IMapper _mapper;

        public GetSubmissionByIdHandler(
            ISubmissionRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SubmissionResponseDto> Handle(
            GetSubmissionByIdQuery request,
            CancellationToken cancellationToken)
        {
            // שולפים לפי מזהה ההגשה
            var submission = await _repository.GetByIdAsync(
                request.SubmissionId,
                cancellationToken);

            if (submission is null)
                throw new NotFoundException(nameof(Submission), request.SubmissionId);

            // בדיקה שההגשה שייכת לתלמידה הספציפית
            if (submission.StudentId != request.StudentId)
                throw new NotFoundException(
                    "Submission does not belong to this student.",
                    request.SubmissionId);

            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
