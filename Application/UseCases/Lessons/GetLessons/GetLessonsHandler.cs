using MediatR;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    public class GetLessonsHandler
        : IRequestHandler<GetLessonsQuery, IReadOnlyList<Lesson>>
    {
        private readonly ILessonRepository _repository;

        public GetLessonsHandler(ILessonRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<Lesson>> Handle(
            GetLessonsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
    }
}

