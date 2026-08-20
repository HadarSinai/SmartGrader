using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartGrader.Api.BackgroundServices;
using SmartGrader.Api.Middlewares;
using SmartGrader.Application;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT token returned from /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddScoped<IGradeSubmissionJob, AiWorker>();
builder.Services.AddScoped<ILogCleanupJob, LogCleanupJob>();

// --- Authentication: JWT Bearer ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// --- Rate limiting: brute-force protection on auth endpoints (per client IP) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // --- מדיניות "ai": פעולות בתשלום שמורה מפעילה בלחיצה ---
    // ⚠️ חלוקה לפי *משתמש* ולא לפי IP, בשונה מ-"auth": כל המורות בבית ספר יושבות מאחורי
    // אותו NAT, וחלוקה לפי IP הייתה נותנת להן מכסה משותפת אחת. כאן ההגנה היא מפני הוצאה
    // כספית של אותה משתמשת (קליק כפול, כפתור תקוע), לא מפני תוקף אנונימי.
    // QueueLimit = 0 בכוונה: להחזיר 429 מיד ולא להחזיק את המורה ממתינה מול מסך תקוע.
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// --- Seed the admin user (from the AdminUser configuration section) ---
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminUsername = config["AdminUser:Username"];
    var adminPassword = config["AdminUser:Password"];

    if (!string.IsNullOrWhiteSpace(adminUsername) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var users = scope.ServiceProvider.GetRequiredService<SmartGrader.Domain.Abstractions.IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<SmartGrader.Application.Common.Interfaces.IPasswordHasherService>();
        var uow = scope.ServiceProvider.GetRequiredService<SmartGrader.Domain.Abstractions.IUnitOfWork>();

        if (!await users.ExistsByUsernameAsync(adminUsername))
        {
            var admin = SmartGrader.Domain.Entities.User.Create(
                adminUsername,
                hasher.Hash(adminPassword),
                config["AdminUser:FullName"] ?? "Admin",
                SmartGrader.Domain.Entities.UserRole.Admin);

            await users.AddAsync(admin);
            await uow.SaveChangesAsync();
        }
    }
}

// Swagger — development only (do not expose the API surface in production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard — development only
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Daily log-retention cleanup (Logs:RetentionDays, default 30)
RecurringJob.AddOrUpdate<ILogCleanupJob>(
    "logs-cleanup",
    job => job.ExecuteAsync(),
    Cron.Daily);

app.MapControllers();

app.Run();

