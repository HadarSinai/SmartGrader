using MediatR;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.DeleteLesson
{
    public class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLessonHandler(
            ILessonRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteLessonCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (lesson == null)
                throw new KeyNotFoundException($"Lesson {request.Id} not found.");

            await _repository.DeleteAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IRequest בלי טיפוס מחזיר Unit
            return Unit.Value;
        }

        Task IRequestHandler<DeleteLessonCommand>.Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
