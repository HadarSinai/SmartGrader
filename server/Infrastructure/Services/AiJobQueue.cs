using System.Threading.Channels;
using SmartGrader.Application.Services.BackgroundJobs;

namespace SmartGrader.Infrastructure.Services.BackgroundJobs
{
    public class AiJobQueue : IAiJobQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        public ValueTask EnqueueAsync(int submissionId, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(submissionId, ct);

        public ValueTask<int> DequeueAsync(CancellationToken ct)
            => _channel.Reader.ReadAsync(ct);
    }
}
