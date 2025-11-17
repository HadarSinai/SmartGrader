using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.Common.Behaviors;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.Common.Behaviors;
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

            // Pipeline של ולידציה לכל Commands/Queries
            services.AddTransient(typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));
            return services;
        }
    }
}

