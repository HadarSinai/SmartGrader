using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Dto.Username)
                .NotEmpty().WithMessage("Username is required.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
