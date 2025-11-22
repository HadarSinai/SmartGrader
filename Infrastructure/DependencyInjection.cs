using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Domain.Abstractions;
using Domain.Abstractions;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Infrastructure.Repositories;


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
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
