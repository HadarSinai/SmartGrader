
using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionCommandValidator
        : AbstractValidator<CreateSubmissionCommand>
    {
        public CreateSubmissionCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0)
                .WithMessage("StudentId must be greater than 0");

            RuleFor(x => x.Dto)
                .NotNull()
                .WithMessage("Submission data is required");

            RuleFor(x => x.Dto.AssignmentId)
                .GreaterThan(0)
                .WithMessage("AssignmentId must be greater than 0");

            // כשההגשה רב-קובצית (Files מאוכלס) SourceCode הישן לא נדרש — ולהפך.
            // ההתאמה בפועל מול Assignment.ExpectedFiles נעשית ב-Handler (יש גישה ל-DB שם).
            RuleFor(x => x.Dto.SourceCode)
                .NotEmpty()
                .WithMessage("SourceCode is required")
                .MinimumLength(5)
                .WithMessage("SourceCode is too short")
                .When(x => x.Dto.Files is null || x.Dto.Files.Count == 0);

            RuleForEach(x => x.Dto.Files)
                .ChildRules(file =>
                {
                    file.RuleFor(f => f.FileName).NotEmpty().WithMessage("FileName is required");
                    file.RuleFor(f => f.Content).NotEmpty().WithMessage("Content is required");
                })
                .When(x => x.Dto.Files is not null && x.Dto.Files.Count > 0);
        }
    }
}
