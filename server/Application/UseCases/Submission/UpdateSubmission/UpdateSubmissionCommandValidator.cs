using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public class UpdateSubmissionCommandValidator
        : AbstractValidator<UpdateSubmissionCommand>
    {
        public UpdateSubmissionCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0)
                .WithMessage("StudentId must be greater than 0");

            RuleFor(x => x.SubmissionId)
                .GreaterThan(0)
                .WithMessage("SubmissionId must be greater than 0");

            RuleFor(x => x.Dto)
                .NotNull()
                .WithMessage("Update data is required");

            RuleFor(x => x.Dto.SourceCode)
                .NotEmpty()
                .WithMessage("SourceCode is required")
                .MinimumLength(5)
                .WithMessage("SourceCode is too short");
        }
    }
}
