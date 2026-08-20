using FluentValidation;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.UpdateAssignment
{
    public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentCommandValidator()
        {
            RuleFor(x => x.LessonId)
                .GreaterThan(0);

            RuleFor(x => x.AssignmentId)
                .GreaterThan(0);

            RuleFor(x => x.Dto.Title)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Dto.BonusValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Dto.IsBonus)
                .WithMessage("BonusValue must be 0 or greater when IsBonus is true.");

            // ⚠️ גם בעדכון, לא רק ביצירה: PUT עם tests ריק מחק עד עכשיו את כל מקרי הבדיקה
            // בשקט והפך תרגיל קיים ללא-ניתן-לניקוד. ר' AssignmentGradeability
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

            RuleFor(x => x.Dto.MethodName)
                .NotEmpty()
                .WithMessage("MethodName is required when GradingMode is Method")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.Method));

            RuleFor(x => x.Dto.ExpectedFiles)
                .NotEmpty()
                .WithMessage("ExpectedFiles is required when GradingMode is MultiFileMethod")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.MultiFileMethod));

            RuleFor(x => x.Dto.ExpectedFiles)
                .Must(files => files.Any(f => !string.IsNullOrWhiteSpace(f.MethodName)))
                .WithMessage("At least one ExpectedFile must specify a MethodName when GradingMode is MultiFileMethod")
                .When(x => IsMode(x.Dto.GradingMode, GradingMode.MultiFileMethod)
                    && x.Dto.ExpectedFiles is { Count: > 0 });

            RuleForEach(x => x.Dto.ExpectedFiles)
                .ChildRules(file => file.RuleFor(f => f.FileName).NotEmpty().WithMessage("FileName is required"))
                .When(x => x.Dto.ExpectedFiles is { Count: > 0 });
        }

        private static bool IsMode(string gradingMode, GradingMode expected) =>
            Enum.TryParse<GradingMode>(gradingMode, true, out var parsed) && parsed == expected;
    }
}
