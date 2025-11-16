using MediatR;

namespace SmartGrader.Application.UseCases.Lessons.DeleteLesson
{
    public record DeleteLessonCommand(int Id) : IRequest;
}
