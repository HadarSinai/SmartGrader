using FluentValidation;

namespace SmartGrader.Application.UseCases.Courses.UpdateCourse
{
    public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
    {
        public UpdateCourseCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);
        }
    }
}
