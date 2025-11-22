using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.DeleteSubmission
{
    public class DeleteSubmissionCommandValidator : AbstractValidator<DeleteSubmissionCommand>
    {
        public DeleteSubmissionCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than zero.");
        }
    }
}
