using FluentValidation;

namespace SmartGrader.Application.UseCases.Courses.GetCourses
{
    public class GetCoursesQueryValidator : AbstractValidator<GetCoursesQuery>
    {
        public GetCoursesQueryValidator()
        {
        }
    }
}
