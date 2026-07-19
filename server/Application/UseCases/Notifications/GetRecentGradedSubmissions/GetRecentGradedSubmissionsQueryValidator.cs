using FluentValidation;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    public class GetRecentGradedSubmissionsQueryValidator
        : AbstractValidator<GetRecentGradedSubmissionsQuery>
    {
        public GetRecentGradedSubmissionsQueryValidator()
        {
            RuleFor(x => x.Limit).GreaterThan(0);
        }
    }
}
