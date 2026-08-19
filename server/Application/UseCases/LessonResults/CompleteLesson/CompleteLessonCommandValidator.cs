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

        // ⚠️ התקרה תלויה ב-HasBonus, בדיוק כמו ב-LessonResult.CompleteWith. עם InclusiveBetween(0,100)
        // קבוע, מורה שהזינה 130 בשיעור עם בונוס קיבלה 400 — למרות שהישות מתירה עד 150 וה-UI מציע זאת.
        RuleFor(x => x.FinalScore)
            .InclusiveBetween(0, 150).WithMessage("FinalScore must be between 0 and 150 when the lesson has a bonus.")
            .When(x => x.HasBonus);

        RuleFor(x => x.FinalScore)
            .InclusiveBetween(0, 100).WithMessage("FinalScore must be between 0 and 100.")
            .When(x => !x.HasBonus);
    }
}
