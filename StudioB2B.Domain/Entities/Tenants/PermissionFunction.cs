namespace StudioB2B.Domain.Entities;

public class PermissionFunction
{
    public Guid PermissionId { get; set; }
    public int FunctionId { get; set; }

    public Permission Permission { get; set; } = null!;
    public AppFunction Function { get; set; } = null!;
}

