using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignments
{
    public class GetAssignmentsHandler
        : IRequestHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentResponseDto>>
    {
        private readonly IAssignmentRepository _repository;
        private readonly IMapper _mapper;

        public GetAssignmentsHandler(IAssignmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<AssignmentResponseDto>> Handle(
            GetAssignmentsQuery request,
            CancellationToken cancellationToken)
        {
            var assignments = await _repository
                .GetByLessonIdAsync(request.LessonId, cancellationToken);

            var result = assignments ?? new List<Domain.Entities.Assignment>();

            return _mapper.Map<IReadOnlyList<AssignmentResponseDto>>(result);
        }
    }
}
