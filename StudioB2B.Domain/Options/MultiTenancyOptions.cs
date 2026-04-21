namespace StudioB2B.Domain.Options;

public class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    public string MasterDomain { get; set; } = string.Empty;

    public string[] ReservedSubdomains { get; set; } = [];

    public string TenantDbConnectionTemplate { get; set; } = string.Empty;
}
