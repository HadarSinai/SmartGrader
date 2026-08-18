using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.GetAssignmentById
{
    public class GetAssignmentByIdHandler
        : IRequestHandler<GetAssignmentByIdQuery, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _repository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IMapper _mapper;

        public GetAssignmentByIdHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            IMapper mapper)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            GetAssignmentByIdQuery request,
            CancellationToken cancellationToken)
        {
            await LessonAccess.GetOwnedOrThrowAsync(_lessonRepository, request.LessonId, request.TeacherId, cancellationToken);

            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);

            if (assignment is null || assignment.LessonId != request.LessonId)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            return _mapper.Map<AssignmentResponseDto>(assignment);
        }
    }
}
