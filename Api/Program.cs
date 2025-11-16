using SmartGrader.Api;
using SmartGrader.Api.Mapping;
using SmartGrader.Application;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Application.UseCases.Lessons.GetLessons;
using SmartGrader.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddAutoMapper(typeof(LessonProfile).Assembly);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetLessonsQuery).Assembly));

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.MapControllers();
app.Run();

