// SmartGrader.Application/UseCases/Lessons/GetLessonById/GetLessonByIdHandler.cs
using MediatR;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public class GetLessonByIdHandler
        : IRequestHandler<GetLessonByIdQuery, Lesson?>
    {
        private readonly ILessonRepository _repository;

        public GetLessonByIdHandler(ILessonRepository repository)
        {
            _repository = repository;
        }

        public async Task<Lesson?> Handle(
            GetLessonByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
