namespace Kaizen.Api.Services.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Kaizen.Api";
    public string Audience { get; set; } = "Kaizen.Client";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 120;
}
