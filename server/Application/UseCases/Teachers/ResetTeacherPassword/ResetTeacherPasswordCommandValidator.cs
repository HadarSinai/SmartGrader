using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Teachers.ResetTeacherPassword
{
    public class ResetTeacherPasswordCommandValidator : AbstractValidator<ResetTeacherPasswordCommand>
    {
        public ResetTeacherPasswordCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.Dto.NewPassword)
                .Password();
        }
    }
}
