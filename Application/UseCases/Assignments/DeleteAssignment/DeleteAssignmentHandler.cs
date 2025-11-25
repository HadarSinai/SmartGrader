using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.DeleteAssignment
{
    public class DeleteAssignmentHandler
        : IRequestHandler<DeleteAssignmentCommand, Unit>
    {
        private readonly IAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILessonRepository _lessonRepository;

        public DeleteAssignmentHandler(
            IAssignmentRepository repository,
            ILessonRepository lessonRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _lessonRepository = lessonRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1) לוודא שהשיעור קיים
            var lesson = await _lessonRepository.GetByIdAsync(request.LessonId, cancellationToken);
            if (lesson is null)
                throw new NotFoundException(nameof(Lesson), request.LessonId);

            // 2) לוודא שהמשימה קיימת
            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment is null)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            // 3) לוודא שהמשימה הזו שייכת לשיעור נכון
            if (assignment.LessonId != request.LessonId)
                throw new NotFoundException(
                    "Assignment",
                    request.AssignmentId
                );

            // 4) מחיקה
            await _repository.DeleteAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
