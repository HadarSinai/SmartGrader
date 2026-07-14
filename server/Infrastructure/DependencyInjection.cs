


using Hangfire;
using Hangfire.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure.Services.CodeRunner;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Repositories;
using SmartGrader.Infrastructure.Services.Feedback;
using SmartGrader.Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;



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
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            //  services.AddSingleton<ICompilerService, RoslynCompilerService>();

            services.AddHttpClient<IFeedbackService, OpenAiFeedbackService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(20);
            });

            services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));

            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

            services.AddHttpClient<ICodeRunnerService, Judge0CodeRunner>();
            services.Configure<Judge0Options>(configuration.GetSection("Judge0"));

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage());

            services.AddHangfireServer();

            return services;
        }
    }
}
