//using Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using SmartGrader;



//var builder = WebApplication.CreateBuilder(args);

//// חיבור למסד הנתונים (SQLite)
//builder.Services.AddDbContext<GradeSheetContext>(options =>
//    options.UseSqlite("Data Source=GradeSheet.db"));

//// הוספת Controllers
//builder.Services.AddControllers();

//// הוספת Swagger (תיעוד ל־API)
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// הפעלת Swagger בזמן פיתוח
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//app.UseAuthorization();
//app.MapControllers();

//app.Run();


//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
using SmartGrader.Infrastructure;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
// Handlers מה-Application:
builder.Services.AddScoped<CompleteLessonHandler>();

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.MapControllers();
app.Run();

