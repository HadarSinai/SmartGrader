using MediatR;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    // TeacherId — תמיד בעלים קונקרטי (CurrentUserId), אף פעם לא null גם עבור מנהל/ת.
    public record CreateLessonCommand(CreateLessonRequestDto Dto, int TeacherId) : IRequest<LessonResponseDto>;
}

