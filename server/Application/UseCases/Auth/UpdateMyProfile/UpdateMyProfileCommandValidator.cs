using FluentValidation;

namespace SmartGrader.Application.UseCases.Auth.UpdateMyProfile
{
    public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileCommandValidator()
        {
            RuleFor(x => x.CurrentUserId).GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("Full name is required.");

            // אותם כללים בדיוק כמו ב-UpdateTeacherCommandValidator, כולל NotEmpty: מורה בלי
            // מייל היא מורה שלא תוכל לשחזר לעצמה סיסמה, ואין טעם לאפשר לה לרוקן את השדה
            // מהאזור האישי אחרי שהמנהלת חויבה למלא אותו.
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not a valid address.")
                .MaximumLength(200).WithMessage("Email must be at most 200 characters long.");

            // ⚠️ אין כאן כלל לשם המשתמש — הוא בלתי-משתנה במכוון ואינו חלק מה-DTO
            // (אין <c>SetUsername</c> על <c>User</c>).
        }
    }
}
