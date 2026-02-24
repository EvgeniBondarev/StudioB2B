using StudioB2B.Web.Components.Entity;
using StudioB2B.Shared.DTOs;

public class MarketplaceClientEntityMeta : IEntityMeta<MarketplaceClientDto>
{
    public string EntityName => "MarketplaceClient";
    public string EntityDisplayName => "Клиент маркетплейса";
    public string? Icon => "AddBusiness";
    public List<EntityField> Fields { get; } = new()
    {
        new EntityField { Name = nameof(MarketplaceClientDto.Name), DisplayName = "Название", IsRequired = true },
        new EntityField { Name = nameof(MarketplaceClientDto.ApiId), DisplayName = "API Id", IsRequired = true },
        new EntityField { Name = nameof(MarketplaceClientDto.ClientTypeName), DisplayName = "Тип", IsEditable = false },
        new EntityField { Name = nameof(MarketplaceClientDto.ModeName), DisplayName = "Режим", IsEditable = false },
        new EntityField { Name = nameof(MarketplaceClientDto.Currency), DisplayName = "Валюта" },
        new EntityField { Name = nameof(MarketplaceClientDto.Company), DisplayName = "Компания" },
        new EntityField { Name = nameof(MarketplaceClientDto.Country), DisplayName = "Страна" },
        new EntityField { Name = nameof(MarketplaceClientDto.INN), DisplayName = "ИНН" },
        new EntityField { Name = nameof(MarketplaceClientDto.LegalName), DisplayName = "Юр. лицо" },
        new EntityField { Name = nameof(MarketplaceClientDto.OzonName), DisplayName = "Ozon Имя" },
        new EntityField { Name = nameof(MarketplaceClientDto.OGRN), DisplayName = "ОГРН" },
    };
    public Func<MarketplaceClientDto, object?> GetId => c => c.Id;
}
