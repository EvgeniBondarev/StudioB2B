using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Адрес доставки получателя заказа.
/// </summary>
[Display(Name = "Адрес")]
public class Address : IBaseEntity
{
    [Display(Name = "Идентификатор адреса")]
    public Guid Id { get; set; }

    [Display(Name = "Город")]
    public string? City { get; set; }

    [Display(Name = "Регион")]
    public string? Region { get; set; }

    [Display(Name = "Улица")]
    public string? Street { get; set; }

    [Display(Name = "Дом")]
    public string? House { get; set; }

    [Display(Name = "Квартира")]
    public string? Apartment { get; set; }

    [Display(Name = "Почтовый индекс")]
    public string? PostalCode { get; set; }

    [Display(Name = "Комментарий к адресу")]
    public string? Comment { get; set; }
}
