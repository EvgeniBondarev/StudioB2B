namespace StudioB2B.Domain.Options;

public class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    public string MasterDomain { get; set; } = "studiob2b.ru";

    public string[] ReservedSubdomains { get; set; } = ["app", "www", "api", "admin", "master", "minio"];

    public string TenantDbConnectionTemplate { get; set; } = "Server=localhost;Database={0};User=root;Password=root;Allow User Variables=true;AllowPublicKeyRetrieval=True;";
}
