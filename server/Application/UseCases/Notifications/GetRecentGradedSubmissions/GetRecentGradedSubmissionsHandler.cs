using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    public class GetRecentGradedSubmissionsHandler
        : IRequestHandler<GetRecentGradedSubmissionsQuery, IReadOnlyList<SubmissionResponseDto>>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IMapper _mapper;

        public GetRecentGradedSubmissionsHandler(ISubmissionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<SubmissionResponseDto>> Handle(
            GetRecentGradedSubmissionsQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _repository.GetRecentGradedAsync(request.Limit, cancellationToken);
            return _mapper.Map<IReadOnlyList<SubmissionResponseDto>>(items);
        }
    }
}
