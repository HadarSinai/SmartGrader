using FluentValidation;

namespace SmartGrader.Application.UseCases.Logs.GetLogs
{
    public class GetLogsQueryValidator : AbstractValidator<GetLogsQuery>
    {
        public GetLogsQueryValidator()
        {
            RuleFor(x => x.MaxCount)
                .GreaterThan(0)
                .WithMessage("MaxCount must be greater than 0.");
        }
    }
}
