using FluentValidation;
using SmartGrader.Application.Common.Validation;

namespace SmartGrader.Application.UseCases.Teachers.CreateTeacher
{
    public class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
    {
        public CreateTeacherCommandValidator()
        {
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("Full name is required.");

            // ⚠️ .Username() ו-.Password() ולא כללים מקומיים: ImportStudentsHandler שכתב אותם
            // מחדש בשורה איבד בדרך את בדיקת התווים העבריים.
            RuleFor(x => x.Dto.Username)
                .Username();

            RuleFor(x => x.Dto.Password)
                .Password();

            // המייל חובה למורה — הוא מזהה השחזור שדרכו היא תחזור לחשבון שלה.
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not a valid address.")
                .MaximumLength(200).WithMessage("Email must be at most 200 characters long.");
        }
    }
}
