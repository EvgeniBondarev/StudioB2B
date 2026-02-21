namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// Настройки Multi-Tenancy
/// </summary>
public class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Мастер-домен (например, studiob2b.ru) - все субдомены которого являются тенантами
    /// </summary>
    public string MasterDomain { get; set; } = "localhost";

    /// <summary>
    /// Субдомен по умолчанию для разработки
    /// </summary>
    public string DefaultSubdomain { get; set; } = "demo";

    /// <summary>
    /// Субдомены которые НЕ являются тенантами (app, www, api, admin)
    /// </summary>
    public string[] ReservedSubdomains { get; set; } = ["app", "www", "api", "admin", "master"];

    /// <summary>
    /// Шаблон connection string для новых тенантов ({0} = имя базы)
    /// </summary>
    public string TenantDbConnectionTemplate { get; set; } =
        "Server=localhost;Database={0};User=root;Password=root;Allow User Variables=true;AllowPublicKeyRetrieval=True;";
}
