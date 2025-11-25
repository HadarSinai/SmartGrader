using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            var submissions = await _repository.GetAllAsync(cancellationToken);

            return _mapper.Map<IReadOnlyList<SubmissionResponseDto>>(submissions);
        }
    }
}
