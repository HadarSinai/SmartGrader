using SmartGrader.Api.BackgroundServices;
using SmartGrader.Api.Middlewares;
using SmartGrader.Application;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure;
using SmartGrader.Infrastructure.Services.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Background AI processing
builder.Services.AddSingleton<IAiJobQueue, AiJobQueue>();
builder.Services.AddHostedService<AiWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

