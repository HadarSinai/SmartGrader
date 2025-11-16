using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public record CreateLessonCommand(
        string Name,
        string Subject,
        DateTime LessonDate,
        string TeacherName
    ) : IRequest<Lesson>;
}

