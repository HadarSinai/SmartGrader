using FluentValidation;

namespace SmartGrader.Application.UseCases.Teachers.UpdateTeacher
{
    public class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
    {
        public UpdateTeacherCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("Full name is required.");

            // אין כאן .Username(): שם המשתמש אינו ניתן לעריכה, והוא אינו חלק מה-DTO.
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is not a valid address.")
                .MaximumLength(200).WithMessage("Email must be at most 200 characters long.");
        }
    }
}
