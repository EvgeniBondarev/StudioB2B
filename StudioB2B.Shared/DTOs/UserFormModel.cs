using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Shared.DTOs;

public class UserFormModel
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите фамилию")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя")]
    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<string> SelectedRoles { get; set; } = [];
}

