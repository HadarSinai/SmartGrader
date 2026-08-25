


using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Application.Services.Feedback;
using SmartGrader.Application.Services.Notifications;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Infrastructure.Services.CodeAnalysis;
using SmartGrader.Infrastructure.Services.CodeRunner;
using SmartGrader.Infrastructure.Data;
using SmartGrader.Infrastructure.Repositories;
using SmartGrader.Infrastructure.Services.Feedback;
using SmartGrader.Infrastructure.Services.App;
using SmartGrader.Infrastructure.Services.Auth;
using SmartGrader.Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;



namespace SmartGrader.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<GradeSheetContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("Default")));

            services.AddScoped<ILessonResultRepository, LessonResultRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<IAssignmentRepository, AssignmentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ISchoolClassRepository, SchoolClassRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // בדיקת הדרישות המבניות — מקומית, מיידית וללא עלות. רצה לפני Judge0, כך שהגשה
            // שלא עומדת בדרישה חוסמת אינה צורכת מכסת הרצה בכלל.
            services.AddScoped<ICodeAnalysisService, RoslynCodeAnalysisService>();

            services.AddHttpClient<IFeedbackService, OpenAiFeedbackService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(20);
            });

            // ⚠️ timeout קצר יותר מהמשוב בכוונה: המשוב הוא עבודת רקע שאיש לא ממתין לה,
            // ואילו כאן המורה עומדת מול המסך ואחרי ההצעות עוד מחכה לה סבב הרצות ב-Judge0.
            services.AddHttpClient<ITestCaseSuggestionService, OpenAiTestCaseSuggestionService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(15);
            });

            services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));

            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

            // ⚠️ נרשם כמופע ולא דרך Configure<T>/IOptions: ClassSignalThresholds יושב ב-Application,
            // ושכבת Application אינה מושכת את Microsoft.Extensions.Options. הקוראת (ClassSignalDetector)
            // מקבלת את ה-POCO ישירות.
            services.AddSingleton(
                configuration.GetSection("Notifications:ClassSignals").Get<ClassSignalThresholds>()
                ?? new ClassSignalThresholds());
            services.AddScoped<ClassSignalDetector>();

            services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddSingleton<IClientUrlProvider, ClientUrlProvider>();

            var judge0Options = configuration.GetSection("Judge0").Get<Judge0Options>() ?? new Judge0Options();
            services.AddHttpClient<ICodeRunnerService, Judge0CodeRunner>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(judge0Options.HttpTimeoutSeconds);
            });
            services.Configure<Judge0Options>(configuration.GetSection("Judge0"));

            // ⚠️ אחסון קבוע, לא UseInMemoryStorage: עם אחסון בזיכרון כל הגשה שממתינה בתור
            // נעלמת בשקט בעת הפעלה מחדש של השרת — היא לעולם לא נבדקת ואף אחד לא מקבל הודעה.
            // קובץ נפרד מ-GradeSheet.db בכוונה: Hangfire כותב לתור בתדירות גבוהה, ו-SQLite נועל
            // ברמת הקובץ — שיתוף הקובץ עם EF היה יוצר תחרות נעילות מיותרת.
            var hangfireConnection =
                configuration.GetConnectionString("Hangfire") ?? "Data Source=hangfire.db";

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage(hangfireConnection));

            services.AddHangfireServer();

            return services;
        }
    }
}
