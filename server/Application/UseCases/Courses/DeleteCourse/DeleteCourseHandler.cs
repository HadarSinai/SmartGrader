using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Courses.DeleteCourse
{
    public class DeleteCourseHandler : IRequestHandler<DeleteCourseCommand>
    {
        private readonly ICourseRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCourseHandler(ICourseRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
        {
            var course = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (course is null || (request.TeacherId.HasValue && course.TeacherId != request.TeacherId.Value))
                throw new NotFoundException(nameof(Course), request.Id);

            if (course.Lessons.Count > 0)
                throw new BusinessRuleException("לא ניתן למחוק קורס שיש בו שיעורים");

            await _repository.DeleteAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
