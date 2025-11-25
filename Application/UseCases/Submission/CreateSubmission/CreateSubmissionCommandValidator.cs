using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
    {
        public CreateSubmissionCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0)
                .WithMessage("StudentId is required");

            RuleFor(x => x.Dto.AssignmentId)
                .GreaterThan(0)
                .WithMessage("AssignmentId is required");

            RuleFor(x => x.Dto.SourceCode)
                .NotEmpty()
                .WithMessage("SourceCode is required");

            RuleFor(x => x.Dto.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }
}
