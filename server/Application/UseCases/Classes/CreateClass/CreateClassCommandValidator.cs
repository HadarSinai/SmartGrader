using FluentValidation;

namespace SmartGrader.Application.UseCases.Classes.CreateClass
{
    public class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
    {
        public CreateClassCommandValidator()
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(50);

            RuleFor(x => x.Dto.AcademicYear)
                .InclusiveBetween(5000, 6000).WithMessage("AcademicYear must be a Hebrew year (5000–6000)");
        }
    }
}
