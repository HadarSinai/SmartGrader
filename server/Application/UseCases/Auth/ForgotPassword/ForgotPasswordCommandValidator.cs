using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            // ⚠️ בדיקת *מבנה* בלבד. היא נכשלת על "abc" ועוברת על כל כתובת תקינה, בין שהיא
            // רשומה במערכת ובין שלא — ולכן אינה מדליפה דבר. כל בדיקה שתלויה בקיום החשבון
            // חייבת להישאר ב-handler, שם התשובה זהה בשני המקרים.
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("יש להזין כתובת מייל")
                .EmailAddress().WithMessage("כתובת המייל אינה תקינה")
                .MaximumLength(256).WithMessage("כתובת המייל ארוכה מדי");
        }
    }
}
