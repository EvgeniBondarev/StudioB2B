using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Модель формы для создания/редактирования роли
/// </summary>
public class RoleFormModel
{
    [Required(ErrorMessage = "Укажите название роли")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "От 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;
}
