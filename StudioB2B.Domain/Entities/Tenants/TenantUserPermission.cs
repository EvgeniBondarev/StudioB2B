namespace StudioB2B.Domain.Entities;

public class TenantUserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }

    public TenantUser User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

