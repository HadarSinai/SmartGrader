using MediatR;
using SmartGrader.Application.Dtos.Courses;

namespace SmartGrader.Application.UseCases.Courses.GetCourses
{
    // TeacherId — null = מנהל/ת (רואה הכל); אחרת מסונן לקורסים של המורה בלבד. אין ערך ברירת מחדל בכוונה.
    public record GetCoursesQuery(int? TeacherId) : IRequest<IReadOnlyList<CourseResponseDto>>;
}
