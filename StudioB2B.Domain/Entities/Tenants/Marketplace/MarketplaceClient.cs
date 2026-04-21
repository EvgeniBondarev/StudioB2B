using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>
    /// Список активных режимов клиента.
    /// В текущей версии БД поддерживается максимум 2 режима, поэтому в набор будет попадать до двух сущностей.
    /// </summary>
    [Display(Name = "Режимы клиента")]
    [NotMapped]
    public List<MarketplaceClientMode> Modes
    {
        get
        {
            var list = new List<MarketplaceClientMode>(capacity: 2);
            if (Mode != null) list.Add(Mode);
            if (Mode2 != null && (Mode is null || Mode2.Id != Mode.Id)) list.Add(Mode2);
            return list;
        }
        set
        {
            var items = value ?? [];

            Mode = items.Count > 0 ? items[0] : null;
            ModeId = Mode?.Id;

            Mode2 = items.Count > 1 ? items[1] : null;
            ModeId2 = Mode2?.Id;
        }
    }

    // Persisted fields (backing storage for Modes). Keep them hidden from IntelliSense.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Guid? ModeId { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public MarketplaceClientMode? Mode { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [ForeignKey(nameof(Mode2))]
    public Guid? ModeId2 { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [ForeignKey(nameof(ModeId2))]
    public MarketplaceClientMode? Mode2 { get; set; }

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
