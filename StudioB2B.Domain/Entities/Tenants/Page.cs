namespace StudioB2B.Domain.Entities;

/// <summary>
/// Страница (раздел) тенанта. Заполняется из <see cref="StudioB2B.Domain.Constants.PageEnum"/>.
/// </summary>
public class Page
{
    public int Id { get; set; }
    /// <summary>Enum name — used as JWT role claim.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Human-readable Russian label from [Description] attribute.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<PageColumn> Columns { get; set; } = new List<PageColumn>();
    public ICollection<AppFunction> Functions { get; set; } = new List<AppFunction>();
    public ICollection<PermissionPage> PermissionPages { get; set; } = new List<PermissionPage>();
}
