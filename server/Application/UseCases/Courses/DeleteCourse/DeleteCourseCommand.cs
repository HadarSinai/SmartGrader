using MediatR;

namespace SmartGrader.Application.UseCases.Courses.DeleteCourse
{
    public record DeleteCourseCommand(int Id, int? TeacherId) : IRequest;
}
