using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    public class DeleteSubmissionCommandValidator
        : AbstractValidator<DeleteSubmissionCommand>
    {
        public DeleteSubmissionCommandValidator()
        {
            RuleFor(x => x.StudentId)
               .GreaterThan(0)
               .WithMessage("Student ID must be greater than 0.");

            RuleFor(x => x.SubmissionId)
                .GreaterThan(0)
                .WithMessage("Submission ID must be greater than 0.");
        }
    }
}
