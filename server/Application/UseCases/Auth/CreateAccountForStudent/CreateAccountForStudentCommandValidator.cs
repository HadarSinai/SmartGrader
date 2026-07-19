using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Auth.CreateAccountForStudent
{
    public class CreateAccountForStudentCommandValidator
        : AbstractValidator<CreateAccountForStudentCommand>
    {
        public CreateAccountForStudentCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.Dto.Username)
                .Username();

            RuleFor(x => x.Dto.Password)
                .Password();
        }
    }
}
