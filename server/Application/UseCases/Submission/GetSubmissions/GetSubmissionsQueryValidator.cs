using FluentValidation;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public class GetSubmissionsQueryValidator
        : AbstractValidator<GetSubmissionsQuery>
    {
        public GetSubmissionsQueryValidator()
        {

        }
    }
}
