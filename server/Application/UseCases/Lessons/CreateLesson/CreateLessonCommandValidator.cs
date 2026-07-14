using FluentValidation;
using SmartGrader.Application.Common.HebrewDate;


namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
    {
        public CreateLessonCommandValidator()
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.Subject)
                .NotEmpty().WithMessage("Subject is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.TeacherName)
                .NotEmpty().WithMessage("TeacherName is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.HebrewYear)
                .InclusiveBetween(5000, 6000).WithMessage("שנה עברית לא תקינה");

            RuleFor(x => x.Dto.HebrewMonth)
                .InclusiveBetween(1, 13);

            RuleFor(x => x.Dto.HebrewDay)
                .InclusiveBetween(1, 30);

            RuleFor(x => x.Dto)
                .Must(d => HebrewDateConverter.IsValidHebrewDate(d.HebrewYear, d.HebrewMonth, d.HebrewDay))
                .WithMessage("התאריך העברי אינו קיים");
        }
    }
}
