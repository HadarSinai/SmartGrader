namespace SmartGrader.Application.Services.BackgroundJobs;

public interface IGradeSubmissionJob
{
    Task ExecuteAsync(int submissionId);
}
