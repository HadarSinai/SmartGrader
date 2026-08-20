using FluentValidation;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.SuggestTestCases
{
    public class SuggestTestCasesCommandValidator : AbstractValidator<SuggestTestCasesCommand>
    {
        public SuggestTestCasesCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("LessonId must be greater than 0");

            // בלי תיאור למודל אין על מה לבסס הצעה, והוא ימציא תרגיל משלו — הצעות שנראות
            // סבירות לגמרי ואין להן שום קשר למה שהמורה מלמדת.
            RuleFor(x => x.Dto.Description)
                .NotEmpty()
                .WithMessage("כדי להציע מקרי בדיקה צריך למלא את תיאור התרגיל.");

            // תקרה על מספר המקרים — ר' SuggestTestCasesLimits (עלות + סקירוּת).
            RuleFor(x => x.Dto.Count)
                .InclusiveBetween(SuggestTestCasesLimits.MinCount, SuggestTestCasesLimits.MaxCount)
                .WithMessage($"אפשר לבקש בין {SuggestTestCasesLimits.MinCount} ל-{SuggestTestCasesLimits.MaxCount} מקרי בדיקה.");

            RuleFor(x => x.Dto.GradingMode)
                .NotEmpty().WithMessage("GradingMode is required")
                .IsEnumName(typeof(GradingMode), caseSensitive: false)
                .WithMessage("GradingMode must be one of: FullProgram, Method, MultiFileMethod");

            RuleFor(x => x.Dto.MethodName)
                .NotEmpty()
                .WithMessage("MethodName is required when GradingMode is Method")
                .When(x => Enum.TryParse<GradingMode>(x.Dto.GradingMode, true, out var parsed)
                           && parsed == GradingMode.Method);
        }
    }
}
