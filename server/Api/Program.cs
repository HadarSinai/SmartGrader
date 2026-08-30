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
using SmartGrader.Application.Services.Notifications;
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
builder.Services.AddScoped<ITeacherDigestJob, TeacherDigestJob>();

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

// --- CORS ---
// ⚠️ רשימת היתר מפורשת בלבד, מתוך App:AllowedOrigins. הערכים האמיתיים יושבים ב-
// appsettings.Development.json (מחוץ ל-git) או במשתני סביבה בייצור.
// הלקוח מזדהה עם כותרת Bearer ולא עם עוגיות, ולכן אין כאן AllowCredentials — וממילא
// אסור לצרף אותו ל-AllowAnyOrigin.
// רשימה ריקה = המדיניות אינה מנפיקה כותרות CORS כלל, כלומר בדיוק ההתנהגות שהייתה עד כה:
// בפיתוח client/proxy.conf.json מגיש את הלקוח ואת ה-API מאותו מקור ואין בקשה חוצת-מקורות.
const string CorsPolicyName = "SmartGraderClient";
var allowedOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length == 0)
            return;

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- Rate limiting: brute-force protection on auth endpoints (per client IP) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // --- מדיניות "auth" ---
    // ⚠️ זו הגנת-הצפה גסה בלבד, ולכן היא רחבה בכוונה: כל המורות בבית ספר יושבות מאחורי
    // אותו NAT, ומכסה של 5 בדקה פירושה שמורה אחת שטעתה בסיסמה חוסמת את כל השאר.
    // ההגנה המדויקת על החשבון עצמה יושבת ב-User.RegisterFailedLogin (נעילה לפי חשבון),
    // שם היא גם אינה פוגעת באף אחת אחרת.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // --- מדיניות "ai": פעולות בתשלום שמורה מפעילה בלחיצה ---
    // ⚠️ חלוקה לפי *משתמש* ולא לפי IP, בשונה מ-"auth": כל המורות בבית ספר יושבות מאחורי
    // אותו NAT, וחלוקה לפי IP הייתה נותנת להן מכסה משותפת אחת. כאן ההגנה היא מפני הוצאה
    // כספית של אותה משתמשת (קליק כפול, כפתור תקוע), לא מפני תוקף אנונימי.
    // QueueLimit = 0 בכוונה: להחזיר 429 מיד ולא להחזיק את המורה ממתינה מול מסך תקוע.
    //
    // 🔴 החלוקה הזו תלויה בסדר ה-middleware: app.UseRateLimiter() חייב לרוץ *אחרי*
    // UseAuthentication, אחרת httpContext.User ריק מ-claims והמפתח נופל תמיד ל-IP —
    // כלומר בדיוק ההתנהגות שההערה למעלה נכתבה כדי למנוע. ר' סדר ה-pipeline למטה.
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
//
// הזריעה פועלת רק כאשר שם המשתמש עדיין אינו קיים (ExistsByUsernameAsync למטה).
//
// ⚠️ הזורע *אינו* דורס סיסמה או מייל קיימים בכוונה. דריסה לפי הקונפיגורציה הייתה מאפסת
// בשקט את סיסמת המנהלת בכל הפעלה מחדש, לערך שיושב בקובץ קונפיגורציה — ומחזירה מייל ישן
// אחרי שהמנהלת כבר עדכנה אותו.
//
// AdminUser:Email נכתב לשורה כאן, וזה מה שמאפשר למנהלת לשחזר את הסיסמה שלה בעצמה דרך
// POST /api/auth/forgot-password — עד לפני כן היא הייתה החשבון היחיד בלי שום דרך חזרה,
// כי אין מעליה מורה שתאפס אותו.
//
// ⚠️ מגבלה שנשארה: שורת מנהלת שנזרעה *לפני* מיגרציית AddUserEmail מחזיקה Email=NULL,
// ו-GetByEmailAsync לעולם אינה מתאימה ל-NULL. המילוי למטה סוגר את זה כשיש מייל בקונפיגורציה,
// ובדיקת האתחול שאחריו צועקת כשאין — ר' B-50.
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

        var existingAdmin = await users.GetByUsernameAsync(adminUsername);

        if (existingAdmin is null)
        {
            var admin = SmartGrader.Domain.Entities.User.Create(
                adminUsername,
                hasher.Hash(adminPassword),
                config["AdminUser:FullName"] ?? "Admin",
                SmartGrader.Domain.Entities.UserRole.Admin,
                config["AdminUser:Email"]);

            await users.AddAsync(admin);
            await uow.SaveChangesAsync();
        }
        else if (string.IsNullOrWhiteSpace(existingAdmin.Email)
                 && !string.IsNullOrWhiteSpace(config["AdminUser:Email"]))
        {
            // מילוי, לא דריסה — ולכן זה אינו סותר את הכלל שלמעלה. שורה שאין בה מייל אינה
            // ערך שהמנהלת בחרה ושדריסה תבטל; היא חשבון שאין ממנו דרך חזרה. מייל קיים —
            // גם אם הוא שונה מהקונפיגורציה — נשאר בדיוק כפי שהוא.
            existingAdmin.SetEmail(config["AdminUser:Email"]);
            await users.UpdateAsync(existingAdmin);
            await uow.SaveChangesAsync();
        }
    }
}

// --- Startup check: an admin with no email can never recover her password (B-50) ---
//
// המילוי למעלה עובד רק כשיש AdminUser:Email בקונפיגורציה ורק על שם המשתמש שמוגדר בה.
// סביבה חדשה, שחזור מגיבוי ישן, או מנהלת שנייה שנוצרה בדרך אחרת — כולן מייצרות שורה
// שהאזהרה הזו היא הסימן היחיד לקיומה. בלעדיה התקלה מתגלה ביום שבו המנהלת שוכחת סיסמה,
// שהוא הרגע הגרוע ביותר לגלות אותה.
using (var scope = app.Services.CreateScope())
{
    var users = scope.ServiceProvider.GetRequiredService<SmartGrader.Domain.Abstractions.IUserRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var stranded = await users.GetByRoleWithoutEmailAsync(SmartGrader.Domain.Entities.UserRole.Admin);

    if (stranded.Count > 0)
    {
        logger.LogWarning(
            "B-50: {Count} admin account(s) hold no email and cannot recover a password: {Usernames}. " +
            "Set AdminUser:Email and restart, or run the one-off UPDATE documented in docs/business-rules.md.",
            stranded.Count,
            string.Join(", ", stranded.Select(u => u.Username)));
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

// לפני האימות: גם דחייה (401/429) צריכה לצאת עם כותרות CORS, אחרת הדפדפן מציג
// שגיאת CORS במקום את הסיבה האמיתית.
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// 🔴 אחרי UseAuthentication, לא לפניו.
// כשזה רץ לפני האימות, httpContext.User עדיין ריק מ-claims, ומדיניות "ai" — שמחלקת לפי
// המשתמשת המחוברת — נפלה תמיד ל-fallback של כתובת ה-IP. כלומר כל המורות מאחורי אותו NAT
// חלקו מכסה אחת של 10 בדקה על פעולות בתשלום, בדיוק מה שהמדיניות נכתבה כדי למנוע.
app.UseRateLimiter();

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

// Daily teacher digest — one email per teacher with signals from yesterday, and *no* email
// at all on a quiet day (see SendTeacherDigestHandler).
//
// ⚠️ השעה נקראת מהקונפיגורציה ומתפרשת כשעון ישראל, ולכן היא נכתבת ל-Hangfire אחרי המרה
// ל-UTC: Cron.Daily(hour) הוא UTC. ברירת המחדל 06:00 מקומי — הדיווח הוא על אתמול, ולכן
// הוא שווה משהו בתחילת יום העבודה ולא בשתיים בלילה.
var digestHourLocal = app.Configuration.GetValue<int?>("Notifications:DigestHourLocal") ?? 6;
RecurringJob.AddOrUpdate<ITeacherDigestJob>(
    "teacher-digest",
    job => job.ExecuteAsync(),
    Cron.Daily(ClassSignalPeriod.LocalHourToUtcHour(digestHourLocal)));

app.MapControllers();

app.Run();

