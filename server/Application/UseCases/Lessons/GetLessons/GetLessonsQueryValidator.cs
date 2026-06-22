using FluentValidation;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    public class GetLessonsQueryValidator : AbstractValidator<GetLessonsQuery>
    {
        public GetLessonsQueryValidator()
        {

        }
    }
}
