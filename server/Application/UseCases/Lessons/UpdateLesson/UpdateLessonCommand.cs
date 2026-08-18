using MediatR;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public record UpdateLessonCommand(
        int Id,
        UpdateLessonRequestDto Dto,
        int? TeacherId
    ) : IRequest<LessonResponseDto>;
}
