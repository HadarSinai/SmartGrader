using Hangfire;
using SmartGrader.Api.BackgroundServices;
using SmartGrader.Api.Middlewares;
using SmartGrader.Application;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddScoped<IGradeSubmissionJob, AiWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();

app.Run();

