using FluentValidation;

namespace SmartGrader.Application.UseCases.Logs.DeleteOldLogs
{
    public class DeleteOldLogsCommandValidator : AbstractValidator<DeleteOldLogsCommand>
    {
        public DeleteOldLogsCommandValidator()
        {
            RuleFor(x => x.Days)
                .GreaterThan(0)
                .WithMessage("Days must be greater than 0.");
        }
    }
}
