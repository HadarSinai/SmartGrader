using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.CreateStudentAccount
{
    public class CreateStudentAccountCommandValidator : AbstractValidator<CreateStudentAccountCommand>
    {
        public CreateStudentAccountCommandValidator()
        {
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("Full name is required.");

            RuleFor(x => x.Dto.ClassName)
                .NotEmpty().WithMessage("Class name is required.");

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        }
    }
}
