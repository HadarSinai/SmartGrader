using FluentValidation;

namespace SmartGrader.Application.UseCases.Courses.CreateCourse
{
    public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
    {
        public CreateCourseCommandValidator()
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);
        }
    }
}
