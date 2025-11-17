using FluentValidation;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
    {
        public UpdateLessonCommandValidator()
        {
            // מזהה השיעור חייב להיות תקין
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Lesson ID must be greater than 0");

            // שם השיעור
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            // המקצוע
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required")
                .MaximumLength(100);

            // שם המורה
            RuleFor(x => x.TeacherName)
                .NotEmpty().WithMessage("Teacher name is required")
                .MaximumLength(100);

            // תאריך השיעור
            RuleFor(x => x.LessonDate)
                .NotEmpty().WithMessage("Lesson date is required")
                .GreaterThan(DateTime.MinValue)
                .WithMessage("Lesson date is invalid");
        }
    }
}
