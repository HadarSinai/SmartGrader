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

        // ⚠️ רק הרצפה נבדקת כאן. התקרה היא 100 ועוד סכום ה-BonusValue של תרגילי הבונוס
        // בשיעור — עובדה שיושבת במסד הנתונים ולא בבקשה — ולכן היא נבדקת ב-handler אחרי
        // שהתרגילים נטענו. הגרסה הקודמת גזרה אותה מ-HasBonus שהלקוח שלח, כלומר הלקוח קבע
        // לעצמו את הטווח החוקי; ואחריה עמד כאן 150 קבוע, שאינו התקרה של אף שיעור בפרט.
        RuleFor(x => x.FinalScore!.Value)
            .GreaterThanOrEqualTo(0).WithMessage("הציון הסופי אינו יכול להיות שלילי.")
            .When(x => x.FinalScore.HasValue);

        RuleFor(x => x.OverrideReason)
            .MaximumLength(500).WithMessage("הסיבה ארוכה מדי (מקסימום 500 תווים).")
            .When(x => !string.IsNullOrWhiteSpace(x.OverrideReason));
    }
}
