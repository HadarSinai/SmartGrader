using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.CreateAssignment
{
    public class CreateAssignmentHandler
        : IRequestHandler<CreateAssignmentCommand, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateAssignmentHandler(
            IAssignmentRepository assignmentRepository,
            ILessonRepository lessonRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _assignmentRepository = assignmentRepository;
            _lessonRepository = lessonRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            CreateAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = await _lessonRepository.GetByIdAsync(request.LessonId, cancellationToken);

            if (lesson is null)
                throw new NotFoundException(nameof(Lesson), request.LessonId);

            var assignment = _mapper.Map<Assignment>(request.Dto);
            assignment.LessonId = request.LessonId;

            await _assignmentRepository.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssignmentResponseDto>(assignment);
        }
    }
}
