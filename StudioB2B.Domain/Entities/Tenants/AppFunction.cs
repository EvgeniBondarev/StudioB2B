namespace StudioB2B.Domain.Entities;

/// <summary>
/// Функция (действие) на странице тенанта. Заполняется из <see cref="StudioB2B.Domain.Constants.FunctionEnum"/>.
/// </summary>
public class AppFunction
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PageId { get; set; }

    public Page Page { get; set; } = null!;
    public ICollection<PermissionFunction> PermissionFunctions { get; set; } = new List<PermissionFunction>();
}

