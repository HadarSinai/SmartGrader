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

            // תרגיל שאין במה לנקד אותו — ר' AssignmentGradeability
            RuleFor(x => x.Dto.Tests)
                .Must(AssignmentGradeability.IsGradeable)
                .WithMessage(AssignmentGradeability.Message);

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
