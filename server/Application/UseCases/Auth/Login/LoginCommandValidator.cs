using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not valid.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
