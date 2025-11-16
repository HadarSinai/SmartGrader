using Application.UseCases;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;

namespace SmartGrader.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateLessonCommand).Assembly));
            return services;
        }
    }
}

