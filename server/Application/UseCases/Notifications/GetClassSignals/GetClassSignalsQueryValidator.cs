using FluentValidation;

namespace SmartGrader.Application.UseCases.Notifications.GetClassSignals
{
    public class GetClassSignalsQueryValidator : AbstractValidator<GetClassSignalsQuery>
    {
        public GetClassSignalsQueryValidator()
        {
            RuleFor(x => x.FromUtc)
                .LessThan(x => x.ToUtc)
                .WithMessage("FromUtc must be earlier than ToUtc.");
        }
    }
}
