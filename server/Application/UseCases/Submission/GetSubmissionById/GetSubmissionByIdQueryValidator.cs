using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    public class GetSubmissionByIdValidator : AbstractValidator<GetSubmissionByIdQuery>
    {
        public GetSubmissionByIdValidator()
        {
            RuleFor(x => x.StudentId)
               .GreaterThan(0)
               .WithMessage("Id must be greater than 0.");
            RuleFor(x => x.SubmissionId)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");
        }
    }
}
