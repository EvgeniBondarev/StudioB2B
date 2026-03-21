namespace StudioB2B.Domain.Entities;

/// <summary>
/// Страница (раздел) тенанта. Заполняется из <see cref="StudioB2B.Domain.Constants.PageEnum"/>.
/// </summary>
public class Page
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<PageColumn> Columns { get; set; } = new List<PageColumn>();
    public ICollection<AppFunction> Functions { get; set; } = new List<AppFunction>();
    public ICollection<PermissionPage> PermissionPages { get; set; } = new List<PermissionPage>();
}

