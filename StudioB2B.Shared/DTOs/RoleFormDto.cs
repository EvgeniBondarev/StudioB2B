using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared;

/// <summary>
/// Модель формы для создания/редактирования роли
/// </summary>
public class RoleFormDto
{
    [Required(ErrorMessage = "Укажите название роли")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "От 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;
}

