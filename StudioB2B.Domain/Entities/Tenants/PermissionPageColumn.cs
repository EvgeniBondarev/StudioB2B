namespace StudioB2B.Domain.Entities;

public class PermissionPageColumn
{
    public Guid PermissionId { get; set; }
    public int PageColumnId { get; set; }

    public Permission Permission { get; set; } = null!;
    public PageColumn PageColumn { get; set; } = null!;
}

