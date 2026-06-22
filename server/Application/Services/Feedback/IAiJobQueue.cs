using System.Threading.Channels;

namespace SmartGrader.Application.Services.BackgroundJobs
{
    public interface IAiJobQueue
    {
        ValueTask EnqueueAsync(int submissionId, CancellationToken ct = default);
        ValueTask<int> DequeueAsync(CancellationToken ct);
    }
}
