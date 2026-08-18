using FluentValidation;

namespace SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

public class CompleteLessonCommandValidator : AbstractValidator<CompleteLessonCommand>
{
    public CompleteLessonCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("Student Id must be greater than 0.");

        RuleFor(x => x.LessonId)
            .GreaterThan(0).WithMessage("Lesson Id must be greater than 0.");

        RuleFor(x => x.FinalScore)
            .InclusiveBetween(0, 100).WithMessage("FinalScore must be between 0 and 100.");
    }
}
