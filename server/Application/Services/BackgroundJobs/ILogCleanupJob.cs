namespace SmartGrader.Application.Services.BackgroundJobs;

public interface ILogCleanupJob
{
    Task ExecuteAsync();
}
