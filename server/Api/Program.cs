//using Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using SmartGrader;

using Application.Common.Mapping;
using SmartGrader.Api.BackgroundServices;
using SmartGrader.Api.Mapping;
using SmartGrader.Api.Middlewares;
using SmartGrader.Application;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Services.BackgroundJobs;
//using SmartGrader.Infrastructure.Services.CodeRunner;
//using SmartGrader.Application.Services.Feedback;
//add Ai Job

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

//�� ����
builder.Services.AddSingleton<IAiJobQueue, AiJobQueue>();
builder.Services.AddHostedService<AiWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

// (�� �� �� Authentication/Authorization �����)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

