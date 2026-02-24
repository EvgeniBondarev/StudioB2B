using StudioB2B.Web.Components.Entity;
using StudioB2B.Shared.DTOs;

public class UserEntityMeta : IEntityMeta<UserListDto>
{
    public string EntityName => "User";
    public string EntityDisplayName => "Пользователь";
    public string? Icon => "Person";
    public List<EntityField> Fields { get; } = new()
    {
        new EntityField { Name = nameof(UserListDto.LastName), DisplayName = "Фамилия", IsRequired = true },
        new EntityField { Name = nameof(UserListDto.FirstName), DisplayName = "Имя", IsRequired = true },
        new EntityField { Name = nameof(UserListDto.MiddleName), DisplayName = "Отчество" },
        new EntityField { Name = nameof(UserListDto.Email), DisplayName = "Email", IsRequired = true },
        new EntityField { Name = nameof(UserListDto.IsActive), DisplayName = "Активен", Type = "bool" },
        new EntityField { Name = nameof(UserListDto.CreatedAtUtc), DisplayName = "Создан", Type = "datetime", IsEditable = false },
    };
    public Func<UserListDto, object?> GetId => u => u.Id;
}
