
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

            RuleFor(x => x.Dto.SourceCode)
                .NotEmpty()
                .WithMessage("SourceCode is required")
                .MinimumLength(5)
                .WithMessage("SourceCode is too short");
        }
    }
}
