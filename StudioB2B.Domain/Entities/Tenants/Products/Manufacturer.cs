using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Производитель товара.
/// </summary>
[Display(Name = "Производитель")]
public class Manufacturer : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор производителя")]
    public Guid Id { get; set; }

    /// <summary>Уникальный префикс (суффикс артикула после '='), например «NKM».</summary>
    [Display(Name = "Префикс")]
    public string Prefix { get; set; } = string.Empty;

    [Display(Name = "Производитель")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Контактная информация")]
    public string? Contact { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Display(Name = "Адрес")]
    public string? Address { get; set; }

    [Display(Name = "Веб-сайт")]
    public string? Website { get; set; }

    [Display(Name = "Рейтинг")]
    public int Rating { get; set; }

    /// <summary>ID из внешнего источника.</summary>
    public int? ExternalId { get; set; }

    public string? ExistName { get; set; }

    public int? ExistId { get; set; }

    [Display(Name = "Домен")]
    public string? Domain { get; set; }

    public int? TecdocSupplierId { get; set; }

    /// <summary>Маркетплейс-префикс для артикулов.</summary>
    public string? MarketPrefix { get; set; }

    public bool IsDeleted { get; set; }
}
