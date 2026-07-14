namespace SmartGrader.Infrastructure.Services.Auth
{
    public class JwtOptions
    {
        public string Key { get; set; } = "";
        public string Issuer { get; set; } = "SmartGrader";
        public string Audience { get; set; } = "SmartGrader";
        public int ExpiresHours { get; set; } = 8;
    }
}
