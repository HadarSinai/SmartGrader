using MediatR;
using SmartGrader.Application.Dtos.Courses;

namespace SmartGrader.Application.UseCases.Courses.GetCourseById
{
    public record GetCourseByIdQuery(int Id, int? TeacherId) : IRequest<CourseResponseDto>;
}
