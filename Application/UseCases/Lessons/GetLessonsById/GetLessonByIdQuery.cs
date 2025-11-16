using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public record GetLessonByIdQuery(int Id) : IRequest<Lesson?>;
}

