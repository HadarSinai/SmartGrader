using FluentValidation;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.CreateAssignment
{
    public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0)
                .WithMessage("LessonId must be greater than 0");

            RuleFor(x => x.Dto.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100);

            RuleFor(x => x.Dto.BonusValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Dto.IsBonus);

            // תרגיל שאין במה לנקד אותו — ר' AssignmentGradeability.
            // ⚠️ "טסט אחד *או* דרישה אחת": תרגיל מחלקות מנוקד על המבנה בלבד ואין לו מה להריץ.
            RuleFor(x => x.Dto)
                .Must(dto => AssignmentGradeability.IsGradeable(dto.Tests, dto.StructuralRules))
                .WithMessage(AssignmentGradeability.Message);

            // הרובריקה מסתכמת בתקרה בדיוק. בתרגיל בונוס התקרה גבוהה מ-100 — ר' Assignment.MaxScore.
            RuleFor(x => x.Dto)
                .Must(dto => AssignmentGradeability.HasValidRubric(
                    AssignmentGradeability.MaxScoreOf(dto.IsBonus, dto.BonusValue),
                    dto.TestsAllocation,
                    dto.Tests,
                    dto.StructuralRules))
                .WithMessage(dto => AssignmentGradeability.RubricMessage(
                    AssignmentGradeability.MaxScoreOf(dto.Dto.IsBonus, dto.Dto.BonusValue)));

            RuleFor(x => x.Dto.StructuralRules)
                .Must(AssignmentGradeability.ScoredRulesCarryPoints)
                .WithMessage(AssignmentGradeability.ScoredPointsMessage);

            RuleForEach(x => x.Dto.StructuralRules).ChildRules(rule =>
            {
                rule.RuleFor(r => r.Kind)
                    .IsEnumName(typeof(RuleKind), caseSensitive: false)
                    .WithMessage("Kind must be one of: MustUse, MustNotUse, AtLeast, AtMost");

                rule.RuleFor(r => r.Construct)
                    .IsEnumName(typeof(CodeConstruct), caseSensitive: false)
                    .WithMessage("Construct is not a known code construct");

                rule.RuleFor(r => r.Severity)
                    .IsEnumName(typeof(RuleSeverity), caseSensitive: false)
                    .WithMessage("Severity must be one of: Blocking, Scored, Advisory");

                // סף חייב להיות חיובי דווקא ב-AtLeast/AtMost; ל-MustUse/MustNotUse אין לו משמעות.
                rule.RuleFor(r => r.Threshold)
                    .GreaterThan(0)
                    .WithMessage("Threshold must be greater than 0 for AtLeast/AtMost rules")
                    .When(r => r.Kind is nameof(RuleKind.AtLeast) or nameof(RuleKind.AtMost));
            });

            RuleFor(x => x.Dto.RetryThreshold)
                .InclusiveBetween(0, Assignment.TotalPoints)
                .WithMessage("RetryThreshold must be between 0 and 100");

            RuleFor(x => x.Dto.GradingMode)
                .NotEmpty().WithMessage("GradingMode is required")
                .IsEnumName(typeof(GradingMode), caseSensitive: false)
                .WithMessage("GradingMode must be one of: FullProgram, Method, MultiFileMethod");

            // מצב Method: חייבים שם מתודה לעטיפת StudentSolution
            RuleFor(x => x.Dto.MethodName)
                .NotEmpty()
                .WithMessage("MethodName is required when GradingMode is Method")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.Method));

            // מצב MultiFileMethod: לפחות קובץ אחד, וקובץ אחד לפחות עם MethodName להרצה
            RuleFor(x => x.Dto.ExpectedFiles)
                .NotEmpty()
                .WithMessage("ExpectedFiles is required when GradingMode is MultiFileMethod")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.MultiFileMethod));

            RuleFor(x => x.Dto.ExpectedFiles)
                .Must(files => files.Any(f => !string.IsNullOrWhiteSpace(f.MethodName)))
                .WithMessage("At least one ExpectedFile must specify a MethodName when GradingMode is MultiFileMethod")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.MultiFileMethod)
                    && x.Dto.ExpectedFiles is { Count: > 0 });

            // בכל מצב עם קבצים (כולל FullProgram רב-קובצי) — שם קובץ הוא שדה חובה בכל שורה
            RuleForEach(x => x.Dto.ExpectedFiles)
                .ChildRules(file => file.RuleFor(f => f.FileName).NotEmpty().WithMessage("FileName is required"))
                .When(x => x.Dto.ExpectedFiles is { Count: > 0 });
        }

        private static bool IsMode(string gradingMode, GradingMode expected) =>
            Enum.TryParse<GradingMode>(gradingMode, true, out var parsed) && parsed == expected;
    }
}
