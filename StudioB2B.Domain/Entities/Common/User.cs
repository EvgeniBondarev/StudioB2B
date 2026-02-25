namespace StudioB2B.Domain.Entities.Common;

public class User : IHasId, ISoftDelete
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<Role> Roles { get; set; } = [];
}
