using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Domain.Abstractions;

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
            var lesson = await LessonAccess.GetOwnedOrThrowAsync(_repository, request.Id, request.TeacherId, cancellationToken);

            await _repository.DeleteAsync(lesson, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IRequest בלי טיפוס מחזיר Unit
            return Unit.Value;
        }
    }
}
