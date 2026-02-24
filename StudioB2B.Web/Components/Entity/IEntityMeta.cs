namespace StudioB2B.Web.Components.Entity;

public interface IEntityMeta<T>
{
    string EntityName { get; }
    string EntityDisplayName { get; }
    string? Icon { get; }
    List<EntityField> Fields { get; }
    Func<T, object?> GetId { get; }
}

public class EntityField
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public Func<object?, string>? Formatter { get; set; }
}
