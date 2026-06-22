using MediatR;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public record GetLessonByIdQuery(int Id) : IRequest<LessonResponseDto>;
}
