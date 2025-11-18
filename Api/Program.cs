
using Domain.Abstractions;
using SmartGrader.Api.Mapping;
using SmartGrader.Api.Middlewares;
using SmartGrader.Application;
using SmartGrader.Infrastructure;
using SmartGrader.Infrastructure.Repositorie;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddAutoMapper(typeof(LessonProfile).Assembly);
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

// (אם יש לך Authentication/Authorization בהמשך)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

