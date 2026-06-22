using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.Common.Behaviors;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using FluentValidation;

namespace SmartGrader.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(CreateLessonCommand).Assembly;

            services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

            services.AddValidatorsFromAssembly(assembly);
          //  services.AddScoped<ICodeRunnerService, LocalProcessCodeRunner>();

            // Pipeline של ולידציה לכל Commands/Queries
            services.AddTransient(typeof(IPipelineBehavior<,>),

                typeof(ValidationBehavior<,>));
            services.AddAutoMapper(assembly);

            return services;
        }
    }
}

