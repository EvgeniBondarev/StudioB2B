using Microsoft.AspNetCore.Identity;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Роль приложения (хранится в базе тенанта)
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
