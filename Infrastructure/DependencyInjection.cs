using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Repositories;

namespace SmartGrader.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // הגדרת החיבור למסד הנתונים
            services.AddDbContext<GradeSheetContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("Default")));
            // אם את עובדת עם SQL Server אפשר לשנות ל:
            // options.UseSqlServer(configuration.GetConnectionString("Default"))

            // רישום ה-Repository וה-UnitOfWork
            services.AddScoped<ILessonResultRepository, LessonResultRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
