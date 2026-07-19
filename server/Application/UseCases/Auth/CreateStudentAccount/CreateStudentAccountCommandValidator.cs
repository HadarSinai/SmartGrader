using FluentValidation;
using SmartGrader.Application.Common.Validation;

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

            RuleFor(x => x.Dto.Username)
                .Username();

            RuleFor(x => x.Dto.Password)
                .Password();
        }
    }
}
