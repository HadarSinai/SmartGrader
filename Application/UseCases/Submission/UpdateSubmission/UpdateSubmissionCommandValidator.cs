using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public class UpdateSubmissionCommandValidator : AbstractValidator<UpdateSubmissionCommand>
    {
        public UpdateSubmissionCommandValidator()
        {
            // מזהה התלמיד
            RuleFor(x => x.StudentId)
                .GreaterThan(0)
                .WithMessage("Student ID must be greater than 0");

            // מזהה ההגשה
            RuleFor(x => x.SubmissionId)
                .GreaterThan(0)
                .WithMessage("Submission ID must be greater than 0");

            // קוד המקור
            RuleFor(x => x.Dto.SourceCode)
                .NotEmpty()
                .WithMessage("Source code is required");

            // ציון
            RuleFor(x => x.Dto.Score)
               .InclusiveBetween(0, 100)
               .WithMessage("Score must be between 0 and 100");

            // הערות
            RuleFor(x => x.Dto.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }
}
