using FluentValidation;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
    {
        public CreateLessonCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);

            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required")
                .MaximumLength(100);

            RuleFor(x => x.TeacherName)
                .NotEmpty().WithMessage("TeacherName is required")
                .MaximumLength(100);

            RuleFor(x => x.LessonDate)
                .NotEmpty().WithMessage("LessonDate is required");
        }
    }
}