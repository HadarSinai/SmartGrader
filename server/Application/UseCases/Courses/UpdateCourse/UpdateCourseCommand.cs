using MediatR;
using SmartGrader.Application.Dtos.Courses;

namespace SmartGrader.Application.UseCases.Courses.UpdateCourse
{
    public record UpdateCourseCommand(int Id, UpdateCourseRequestDto Dto, int? TeacherId) : IRequest<CourseResponseDto>;
}
