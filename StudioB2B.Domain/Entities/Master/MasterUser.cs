namespace StudioB2B.Domain.Entities;

public class MasterUser : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string HashPassword { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public ICollection<MasterUserRole> UserRoles { get; set; } = new List<MasterUserRole>();
}

