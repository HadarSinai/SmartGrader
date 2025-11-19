using FluentValidation;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public class GetLessonByIdValidator : AbstractValidator<GetLessonByIdQuery>
    {
        public GetLessonByIdValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than zero.");
        }
    }
}

