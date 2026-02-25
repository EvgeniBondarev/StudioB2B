namespace StudioB2B.Domain.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "StudioB2B";
    public string Audience { get; set; } = "StudioB2B";
    public int ExpirationDays { get; set; } = 14;
}

