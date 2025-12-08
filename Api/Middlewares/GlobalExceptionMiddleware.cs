
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartGrader.Application.Common.Exceptions;

namespace SmartGrader.Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            // ------------------------- 400 - Validation -------------------------
            catch (AppValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error");

                // תקציר שגיאות יפה וקריא
                var summary = string.Join(" | ",
                    ex.Errors.Select(kvp =>
                        $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));

                var problem = new ValidationProblemDetails(ex.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = summary,
                    Type = "https://httpstatuses.com/400",
                    Instance = context.Request.Path
                };

                AddTraceId(problem, context);
                await WriteProblemDetailsAsync(context, problem);
            }
            // ------------------------- 404 - Not Found -------------------------
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found");

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found.",
                    Detail = ex.Message,
                    Type = "https://httpstatuses.com/404",
                    Instance = context.Request.Path
                };

                AddTraceId(problem, context);
                await WriteProblemDetailsAsync(context, problem);
            }
            // ------------------------- 409 - Unique Constraint -------------------------
            catch (UniqueConstraintException ex)
            {
                _logger.LogWarning(ex, "Unique constraint violation");

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate value – conflict.",
                    Detail = ex.Message,
                    Type = "https://httpstatuses.com/409",
                    Instance = context.Request.Path
                };

                AddTraceId(problem, context);
                await WriteProblemDetailsAsync(context, problem);
            }
            // ------------------------- 500 - Unknown Errors -------------------------
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server error.",
                    Detail = ex.ToString(),//"An unexpected error occurred.",
                    Type = "https://httpstatuses.com/500",
                    Instance = context.Request.Path
                };

                AddTraceId(problem, context);
                await WriteProblemDetailsAsync(context, problem);
            }
        }

        private static void AddTraceId(ProblemDetails problem, HttpContext context)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            problem.Extensions["traceId"] = traceId;
        }

        private static async Task WriteProblemDetailsAsync(
            HttpContext context,
            ProblemDetails problem)
        {
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        }
    }
}

