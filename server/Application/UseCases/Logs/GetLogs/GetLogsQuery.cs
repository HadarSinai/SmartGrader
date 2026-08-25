using MediatR;
using SmartGrader.Application.Dtos.Logs;

namespace SmartGrader.Application.UseCases.Logs.GetLogs
{
    public record GetLogsQuery(int MaxCount = 500) : IRequest<IReadOnlyList<LogResponseDto>>;
}
