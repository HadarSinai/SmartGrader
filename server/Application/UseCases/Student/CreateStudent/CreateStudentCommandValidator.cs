using FluentValidation;

namespace SmartGrader.Application.UseCases.Students.CreateStudent
{
    public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentCommandValidator()
        {
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("FullName is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.ClassId)
                .GreaterThan(0).WithMessage("ClassId is required");
        }
    }
}
