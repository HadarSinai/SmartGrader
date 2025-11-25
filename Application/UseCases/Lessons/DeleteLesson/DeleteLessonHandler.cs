using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.DeleteLesson
{
    public class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand,Unit>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLessonHandler(ILessonRepository repository, IUnitOfWork unitOfWork)
            => (_repository, _unitOfWork) = (repository, unitOfWork);

        public async Task<Unit> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (lesson is null)
                throw new NotFoundException("Lesson", request.Id);

            await _repository.DeleteAsync(lesson, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

    }
}
