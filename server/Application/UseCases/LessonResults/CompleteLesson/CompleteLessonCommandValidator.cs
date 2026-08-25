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

        // ⚠️ כאן רק התקרה המוחלטת. התקרה האמיתית (100 או 150) תלויה בשאלה אם יש בשיעור
        // תרגיל בונוס, וזו עובדה שיושבת במסד הנתונים ולא בבקשה — ולכן היא נבדקת ב-handler
        // אחרי שהתרגילים נטענו. הגרסה הקודמת גזרה אותה מ-HasBonus שהלקוח שלח, כלומר
        // הלקוח קבע לעצמו את הטווח החוקי.
        RuleFor(x => x.FinalScore!.Value)
            .InclusiveBetween(0, 150).WithMessage("הציון הסופי חייב להיות בין 0 ל-150.")
            .When(x => x.FinalScore.HasValue);

        RuleFor(x => x.OverrideReason)
            .MaximumLength(500).WithMessage("הסיבה ארוכה מדי (מקסימום 500 תווים).")
            .When(x => !string.IsNullOrWhiteSpace(x.OverrideReason));
    }
}
