using Microsoft.Extensions.Configuration;
using SmartGrader.Application.Common.Interfaces;

namespace SmartGrader.Infrastructure.Services.App
{
    /// <summary>
    /// קוראת את App:ClientBaseUrl. הערך האמיתי יושב ב-appsettings.Development.json
    /// (מחוץ ל-git) או במשתנה סביבה בייצור; ב-appsettings.json יש רק מציין מקום ריק.
    /// </summary>
    public class ClientUrlProvider : IClientUrlProvider
    {
        public ClientUrlProvider(IConfiguration configuration)
        {
            // לוכסן מסיים נחתך פעם אחת כאן, כדי שכל בונה קישור יוכל לשרשר "/..." בלי
            // לייצר "//" — כתובת שנראית שבורה למי שמקבלת אותה במייל.
            BaseUrl = (configuration["App:ClientBaseUrl"] ?? "").Trim().TrimEnd('/');
        }

        public string BaseUrl { get; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
