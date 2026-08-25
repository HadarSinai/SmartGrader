using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Auth.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Dto.Token)
                .NotEmpty().WithMessage("הקישור אינו תקין");

            // אותם כללים בדיוק כמו בכל מסלול אחר שקובע סיסמה — ר' PasswordPolicy.
            RuleFor(x => x.Dto.NewPassword)
                .Password();
        }
    }
}
