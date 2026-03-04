using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Роль приложения (хранится в базе тенанта)
/// </summary>
[Display(Name = "Роль")]
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
