using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Auth.ChangeMyPassword
{
    public class ChangeMyPasswordCommandValidator : AbstractValidator<ChangeMyPasswordCommand>
    {
        public ChangeMyPasswordCommandValidator()
        {
            RuleFor(x => x.CurrentUserId).GreaterThan(0).WithMessage("Id must be greater than 0.");

            // ⚠️ NotEmpty בלבד על הסיסמה הנוכחית, ובמכוון **לא** .Password(). היא נבדקת מול
            // ה-hash ב-handler, וסיסמה ישנה שנוצרה לפני שהמדיניות הנוכחית נכנסה לתוקף אינה
            // חייבת לעמוד בה. אכיפת המדיניות כאן הייתה חוסמת בדיוק את מי שהכי צריכה להחליף.
            RuleFor(x => x.Dto.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.Dto.NewPassword)
                .NotEmpty().WithMessage("Password is required.")
                .Password();

            // סיסמה חדשה זהה לישנה אינה שינוי. בלי הכלל הזה הפעולה מצליחה ולא עושה כלום,
            // והמשתמשת מקבלת הודעת הצלחה על שינוי שלא קרה.
            RuleFor(x => x.Dto.NewPassword)
                .NotEqual(x => x.Dto.CurrentPassword)
                .WithMessage("The new password must be different from the current one.");
        }
    }
}
