using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenant;

public class TenantUser : IHasId, ISoftDelete
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string Patronymic { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<TenantRole> Roles { get; set; } = [];


}
