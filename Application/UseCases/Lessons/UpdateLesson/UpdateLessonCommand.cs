using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public record UpdateLessonCommand(
        int Id,
        string Name,
        string Subject,
        DateTime LessonDate,
        string TeacherName
    ) : IRequest<Lesson>;
}

