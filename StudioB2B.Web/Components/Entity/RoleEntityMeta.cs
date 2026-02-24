using StudioB2B.Web.Components.Entity;
using StudioB2B.Shared.DTOs;

public class RoleEntityMeta : IEntityMeta<RoleDto>
{
    public string EntityName => "Role";
    public string EntityDisplayName => "Роль";
    public string? Icon => "ManageAccounts";
    public List<EntityField> Fields { get; } = new()
    {
        new EntityField { Name = nameof(RoleDto.Name), DisplayName = "Название", IsRequired = true },
        new EntityField { Name = nameof(RoleDto.Description), DisplayName = "Описание" },
        new EntityField { Name = nameof(RoleDto.IsSystemRole), DisplayName = "Системная", Type = "bool", IsEditable = false },
        new EntityField { Name = nameof(RoleDto.CreatedAtUtc), DisplayName = "Создана", Type = "datetime", IsEditable = false },
    };
    public Func<RoleDto, object?> GetId => r => r.Id;
}
