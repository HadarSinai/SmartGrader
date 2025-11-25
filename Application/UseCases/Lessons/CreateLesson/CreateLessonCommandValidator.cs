using FluentValidation;


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

            RuleFor(x => x.Dto.LessonDate)
                .NotEmpty().WithMessage("LessonDate is required");
        }
    }
}