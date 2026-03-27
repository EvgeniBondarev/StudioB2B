using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared;

public class MasterRegisterModelDto
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль")]
    [MinLength(6, ErrorMessage = "Минимум 6 символов")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Подтвердите пароль")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Введите имя")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "До 100 символов")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Введите фамилию")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "До 100 символов")]
    public string LastName { get; set; } = "";

    [StringLength(100, ErrorMessage = "До 100 символов")]
    public string? MiddleName { get; set; }
}

