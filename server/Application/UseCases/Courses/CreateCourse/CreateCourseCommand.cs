using MediatR;
using SmartGrader.Application.Dtos.Courses;

namespace SmartGrader.Application.UseCases.Courses.CreateCourse
{
    // TeacherId — תמיד בעלים קונקרטי (CurrentUserId), אף פעם לא null גם עבור מנהל/ת.
    public record CreateCourseCommand(CreateCourseRequestDto Dto, int TeacherId) : IRequest<CourseResponseDto>;
}
