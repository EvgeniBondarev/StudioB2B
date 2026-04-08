namespace StudioB2B.Domain.Entities;

public class TenantLoginCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public TenantUser User { get; set; } = null!;
}

