using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Пользователь приложения (хранится в базе тенанта)
/// </summary>
[Display(Name = "Пользователь")]
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {MiddleName}";
}


