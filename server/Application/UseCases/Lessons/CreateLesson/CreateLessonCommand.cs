using MediatR;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public record CreateLessonCommand(CreateLessonRequestDto Dto) : IRequest<LessonResponseDto>;
}

