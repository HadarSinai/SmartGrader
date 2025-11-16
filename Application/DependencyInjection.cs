using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace SmartGrader.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CompleteLessonHandler).Assembly));
            return services;
        }
    }
}

