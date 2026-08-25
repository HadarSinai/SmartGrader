using MediatR;

namespace SmartGrader.Application.UseCases.Logs.DeleteOldLogs
{
    public record DeleteOldLogsCommand(int Days) : IRequest<int>;
}
