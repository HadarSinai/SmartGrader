using FluentValidation;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.VerifyTestCases
{
    public class VerifyTestCasesCommandValidator : AbstractValidator<VerifyTestCasesCommand>
    {
        public VerifyTestCasesCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("LessonId must be greater than 0");

            // אין מה לאמת בלי פתרון — זה לא "אזהרה רכה" אלא חוסר קלט. האזהרה הרכה
            // (אפשר לשמור תרגיל בלי לאמת) חלה על *השמירה*, לא על הכפתור הזה.
            RuleFor(x => x.Dto.ReferenceSolution)
                .Must(files => files is { Count: > 0 } && files.Any(f => !string.IsNullOrWhiteSpace(f.Content)))
                .WithMessage("כדי לבדוק את מקרי הבדיקה צריך להזין פתרון לדוגמה.");

            RuleFor(x => x.Dto.Tests)
                .Must(tests => tests is { Count: > 0 })
                .WithMessage("אין מקרי בדיקה לבדוק.");

            RuleFor(x => x.Dto.GradingMode)
                .NotEmpty().WithMessage("GradingMode is required")
                .IsEnumName(typeof(GradingMode), caseSensitive: false)
                .WithMessage("GradingMode must be one of: FullProgram, Method, MultiFileMethod");

            // אותה דרישה בדיוק כמו ביצירת תרגיל: בלי שם מתודה אין למה לקרוא בעטיפה,
            // וה-Runner היה מייצר קוד שלא מתקמפל ומאשים בזה את הפתרון של המורה.
            RuleFor(x => x.Dto.MethodName)
                .NotEmpty()
                .WithMessage("MethodName is required when GradingMode is Method")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.Method));

            RuleFor(x => x.Dto.ExpectedFiles)
                .Must(files => files is { Count: > 0 } && files.Any(f => !string.IsNullOrWhiteSpace(f.MethodName)))
                .WithMessage("At least one ExpectedFile must specify a MethodName when GradingMode is MultiFileMethod")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.MultiFileMethod));
        }

        private static bool IsMode(string gradingMode, GradingMode expected) =>
            Enum.TryParse<GradingMode>(gradingMode, true, out var parsed) && parsed == expected;
    }
}
