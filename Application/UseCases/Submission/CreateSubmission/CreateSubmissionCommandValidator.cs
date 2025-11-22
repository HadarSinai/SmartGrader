using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
    {
        public CreateSubmissionCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId is required");

            RuleFor(x => x.AssignmentId)
                .GreaterThan(0).WithMessage("AssignmentId is required");

            RuleFor(x => x.SourceCode)
                .NotEmpty().WithMessage("SourceCode is required");

            RuleFor(x => x.Score)
                .GreaterThanOrEqualTo(0).WithMessage("Score must be 0 or higher");

            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }
}
