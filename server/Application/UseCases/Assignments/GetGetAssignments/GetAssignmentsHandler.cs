using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignments
{
    public class GetAssignmentsHandler
        : IRequestHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentResponseDto>>
    {
        private readonly IAssignmentRepository _repository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;

        public GetAssignmentsHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            IStudentRepository studentRepository,
            IMapper mapper)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<AssignmentResponseDto>> Handle(
            GetAssignmentsQuery request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetAccessibleOrThrowAsync(
                _lessonRepository, _studentRepository, request.LessonId, request.TeacherId, request.StudentId, cancellationToken);

            var assignments = await _repository
                .GetByLessonIdAsync(request.LessonId, cancellationToken);

            var result = assignments ?? new List<Domain.Entities.Assignment>();

            return _mapper.Map<IReadOnlyList<AssignmentResponseDto>>(result);
        }
    }
}
