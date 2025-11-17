// SmartGrader.Application/UseCases/Lessons/UpdateLesson/UpdateLessonHandler.cs
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonHandler
        : IRequestHandler<UpdateLessonCommand, Lesson>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLessonHandler(
            ILessonRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Lesson> Handle(
            UpdateLessonCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);


            if (lesson is null)
            {
                // כאן השיעור לא נמצא → זורקים NotFoundException
                throw new NotFoundException(nameof(Lesson), request.Id);
            }

            lesson.Name = request.Name;
            lesson.Subject = request.Subject;
            lesson.LessonDate = request.LessonDate;
            lesson.TeacherName = request.TeacherName;

            await _repository.UpdateAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return lesson;
        }
    }
}
