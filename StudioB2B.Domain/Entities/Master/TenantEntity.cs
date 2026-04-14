namespace StudioB2B.Domain.Entities;

public class TenantEntity : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subdomain { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    /// <summary>Требовать ли код подтверждения при входе (по умолчанию true)</summary>
    public bool RequireLoginCode { get; set; } = true;

    /// <summary>Требовать ли активацию аккаунта по email при создании пользователя (по умолчанию true)</summary>
    public bool RequireEmailActivation { get; set; } = true;

    /// <summary>ID master-пользователя, создавшего этот tenant</summary>
    public Guid? CreatedByUserId { get; set; }

    public MasterUser? CreatedBy { get; set; }
}
