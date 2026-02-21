namespace StudioB2B.Domain.Entities.Tenants;

/// <summary>
/// Роль в мастер-базе (эталон для копирования в базы тенантов)
/// Структура повторяет AspNetRoles
/// </summary>
public class MasterRole
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

