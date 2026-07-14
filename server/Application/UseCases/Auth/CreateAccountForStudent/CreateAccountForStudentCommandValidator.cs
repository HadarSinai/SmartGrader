using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.CreateAccountForStudent
{
    public class CreateAccountForStudentCommandValidator
        : AbstractValidator<CreateAccountForStudentCommand>
    {
        public CreateAccountForStudentCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        }
    }
}
