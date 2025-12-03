using Domain.Abstractions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Repositories;
//using SmartGrader.Infrastructure.Services;


namespace SmartGrader.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<GradeSheetContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("Default")));

            services.AddScoped<ILessonResultRepository, LessonResultRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<IAssignmentRepository, AssignmentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
          //  services.AddSingleton<ICompilerService, RoslynCompilerService>();

            return services;
        }
    }
}
