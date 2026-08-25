namespace SmartGrader.Application.Services.BackgroundJobs;

public interface ITeacherDigestJob
{
    Task ExecuteAsync();
}
