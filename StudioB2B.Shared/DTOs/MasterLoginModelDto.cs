using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared;

public class MasterLoginModelDto
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = "";
}
