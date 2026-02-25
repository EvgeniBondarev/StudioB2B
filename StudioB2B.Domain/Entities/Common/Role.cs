namespace StudioB2B.Domain.Entities.Common;

public class Role : IHasId, IHasName, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<User> Users { get; set; } = [];
}
