using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Получатель заказа (customer из Ozon API).
/// </summary>
[Display(Name = "Получатель")]
public class Recipient : IBaseEntity
{
    [Display(Name = "Идентификатор получателя")]
    public Guid Id { get; set; }

    [Display(Name = "Получатель")]
    public string? Name { get; set; }

    [Display(Name = "Телефон получателя")]
    public string? Phone { get; set; }

    [Display(Name = "Email получателя")]
    public string? Email { get; set; }

    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }
}
