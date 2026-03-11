namespace StudioB2B.Domain.Entities;

public class TenantUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation
    public TenantUser User { get; set; } = null!;
    public TenantRole Role { get; set; } = null!;
}

