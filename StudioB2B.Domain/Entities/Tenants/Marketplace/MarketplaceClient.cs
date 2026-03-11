using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

[Display(Name = "Клиент маркетплейса")]
public class MarketplaceClient : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    [Display(Name = "Название клиента")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "API ID (Client-Id)")]
    public string ApiId { get; set; } = string.Empty;

    [Display(Name = "API ключ (Api-Key)")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "Тип клиента")]
    public Guid? ClientTypeId { get; set; }

    public MarketplaceClientType? ClientType { get; set; }

    [Display(Name = "Режим клиента")]
    public Guid? ModeId { get; set; }

    public MarketplaceClientMode? Mode { get; set; }

    public List<MarketplaceClientSettings> Settings { get; set; } = new();

    public MarketplaceClient1CSettings? Settings1C { get; set; }

    [Display(Name = "Компания")]
    public string? Company { get; set; }

    [Display(Name = "Страна")]
    public string? Country { get; set; }

    [Display(Name = "Валюта")]
    public string? Currency { get; set; }

    [Display(Name = "ИНН")]
    public string? INN { get; set; }

    [Display(Name = "Юридическое наименование")]
    public string? LegalName { get; set; }

    [Display(Name = "Название на Ozon")]
    public string? OzonName { get; set; }

    [Display(Name = "ОГРН")]
    public string? OGRN { get; set; }

    [Display(Name = "Форма собственности")]
    public string? OwnershipForm { get; set; }

    public bool IsDeleted { get; set; }
}
