using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.UpdateSubmission
{
    public class UpdateSubmissionCommandValidator : AbstractValidator<UpdateSubmissionCommand>
    {
        public UpdateSubmissionCommandValidator()
        {
            // מזהה ההגשה חייב להיות תקין
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Submission ID must be greater than 0");

            // מזהה הסטודנט
            RuleFor(x => x.StudentId)
                .GreaterThan(0)
                .WithMessage("Student ID must be greater than 0");

            // מזהה המשימה
            RuleFor(x => x.AssignmentId)
                .GreaterThan(0)
                .WithMessage("Assignment ID must be greater than 0");

            // קוד המקור
            RuleFor(x => x.SourceCode)
                .NotEmpty().WithMessage("Source code is required");

            // ציון
            RuleFor(x => x.Score)
                .InclusiveBetween(0, 100)
                .WithMessage("Score must be between 0 and 100");

            // הערות
            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }
}
