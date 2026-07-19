using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Auth.RegisterTeacher
{
    public class RegisterTeacherCommandValidator : AbstractValidator<RegisterTeacherCommand>
    {
        public RegisterTeacherCommandValidator()
        {
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("Full name is required.");

            RuleFor(x => x.Dto.Username)
                .Username();

            RuleFor(x => x.Dto.Password)
                .Password();
        }
    }
}
