namespace StudioB2B.Domain.Entities;

public class MasterUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation
    public MasterUser User { get; set; } = null!;
    public MasterRole Role { get; set; } = null!;
}

