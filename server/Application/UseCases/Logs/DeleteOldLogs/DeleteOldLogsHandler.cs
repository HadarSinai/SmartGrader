using MediatR;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Logs.DeleteOldLogs
{
    public class DeleteOldLogsHandler : IRequestHandler<DeleteOldLogsCommand, int>
    {
        private readonly ILogRepository _repository;

        public DeleteOldLogsHandler(ILogRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(DeleteOldLogsCommand request, CancellationToken cancellationToken)
        {
            var cutoffUtc = DateTime.UtcNow.AddDays(-request.Days);
            return await _repository.DeleteOlderThanAsync(cutoffUtc, cancellationToken);
        }
    }
}
