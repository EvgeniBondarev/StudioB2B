namespace StudioB2B.Domain.Entities;

/// <summary>
/// Функция (действие) на странице тенанта. Заполняется из <see cref="StudioB2B.Domain.Constants.FunctionEnum"/>.
/// </summary>
public class AppFunction
{
    public int Id { get; set; }
    /// <summary>Enum name — used as JWT role claim.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Human-readable Russian label from [Description] attribute.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public int PageId { get; set; }

    public Page Page { get; set; } = null!;
    public ICollection<PermissionFunction> PermissionFunctions { get; set; } = new List<PermissionFunction>();
}
