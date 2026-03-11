namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Интерфейс для получения информации о текущем тенанте
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// ID текущего тенанта
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Поддомен текущего тенанта
    /// </summary>
    string? Subdomain { get; }

    /// <summary>
    /// Connection string к базе текущего тенанта
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Активен ли тенант
    /// </summary>
    bool IsResolved { get; }
}


