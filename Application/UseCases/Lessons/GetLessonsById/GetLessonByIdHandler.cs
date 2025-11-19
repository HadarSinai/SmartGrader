using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public class GetLessonByIdHandler
        : IRequestHandler<GetLessonByIdQuery, Lesson>
    {
        private readonly ILessonRepository _repository;

        public GetLessonByIdHandler(ILessonRepository repository)
            => _repository = repository;

        public async Task<Lesson> Handle(GetLessonByIdQuery request, CancellationToken ct)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, ct);

            if (lesson is null)
                throw new NotFoundException(nameof(Lesson), request.Id);

            return lesson;
        }
    }
}
