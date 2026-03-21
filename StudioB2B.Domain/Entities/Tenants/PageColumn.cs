namespace StudioB2B.Domain.Entities;

/// <summary>
/// Колонка таблицы на странице тенанта. Заполняется из <see cref="StudioB2B.Domain.Constants.PageColumnEnum"/>.
/// </summary>
public class PageColumn
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PageId { get; set; }

    public Page Page { get; set; } = null!;
    public ICollection<PermissionPageColumn> PermissionPageColumns { get; set; } = new List<PermissionPageColumn>();
}

