using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public class GetSubmissionsHandler
        : IRequestHandler<GetSubmissionsQuery, IReadOnlyList<SubmissionResponseDto>>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IMapper _mapper;

        public GetSubmissionsHandler(
            ISubmissionRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<SubmissionResponseDto>> Handle(
            GetSubmissionsQuery request,
            CancellationToken cancellationToken)
        {
            var submissions = await _repository.GetByStudentIdAsync(
                request.StudentId,
                request.TeacherId,
                cancellationToken);

            // ⚠️ רשימת ההגשות נושאת TestResults מלאים בדיוק כמו הפריט הבודד — אותו סינון.
            return TestVisibility.RedactTestResults(
                _mapper.Map<IReadOnlyList<SubmissionResponseDto>>(submissions),
                request.IsStudentCaller);
        }
    }
}
