using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared.DTOs;

public class RegisterModelDto
{
    [Required(ErrorMessage = "Введите название компании")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "От 2 до 100 символов")]
    public string CompanyName { get; set; } = "";

    [Required(ErrorMessage = "Введите субдомен")]
    [RegularExpression(@"^[a-z0-9][a-z0-9\-]{1,28}[a-z0-9]$|^[a-z0-9]{3}$", ErrorMessage = "Только a-z, 0-9, дефис (3–30 символов)")]
    public string Subdomain { get; set; } = "";

    [Required(ErrorMessage = "Введите email администратора")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string AdminEmail { get; set; } = "";

    [Required(ErrorMessage = "Введите имя")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "До 100 символов")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Введите фамилию")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "До 100 символов")]
    public string LastName { get; set; } = "";

    [StringLength(100, ErrorMessage = "До 100 символов")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Введите пароль")]
    [MinLength(6, ErrorMessage = "Минимум 6 символов")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Подтвердите пароль")]
    public string ConfirmPassword { get; set; } = "";
}
