using FluentValidation;

namespace SmartGrader.Application.UseCases.Notifications.GetRecentGradedSubmissions
{
    public class GetRecentGradedSubmissionsQueryValidator
        : AbstractValidator<GetRecentGradedSubmissionsQuery>
    {
        public GetRecentGradedSubmissionsQueryValidator()
        {
            // ⚠️ תקרה, לא רק GreaterThan(0): בלעדיה ?limit=100000 התקבל כמו שהוא, וכל טעינת
            // פעמון ההתראות שלפה את כל ההגשות שנבדקו אי פעם.
            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 100)
                .WithMessage("מספר ההתראות חייב להיות בין 1 ל-100");
        }
    }
}
