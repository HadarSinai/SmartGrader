// SmartGrader.Application/UseCases/Lessons/CreateLesson/CreateLessonHandler.cs
using MediatR;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public class CreateLessonHandler
        : IRequestHandler<CreateLessonCommand, Lesson>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateLessonHandler(ILessonRepository repository, IUnitOfWork unitOfWork)
           => (_repository, _unitOfWork) = (repository, unitOfWork);

        public async Task<Lesson> Handle(
            CreateLessonCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = new Lesson
            {
                Name = request.Name,
                Subject = request.Subject,
                LessonDate = request.LessonDate,
                TeacherName = request.TeacherName
                // CreatedAt מוגדר אוטומטית ב-ctor / default
            };

            await _repository.AddAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return lesson;
        }
    }
}

