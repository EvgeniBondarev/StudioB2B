namespace StudioB2B.Domain.Entities;

public class PermissionPage
{
    public Guid PermissionId { get; set; }
    public int PageId { get; set; }

    public Permission Permission { get; set; } = null!;
    public Page Page { get; set; } = null!;
}

