namespace StudioB2B.Domain.Entities;

public class TenantEntity : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subdomain { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    /// <summary>ID master-пользователя, создавшего этот tenant</summary>
    public Guid? CreatedByUserId { get; set; }

    public MasterUser? CreatedBy { get; set; }
}
