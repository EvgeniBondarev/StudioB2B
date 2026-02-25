using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum MarketplaceClientTypeEnum
{
    [Description("Ozon")]
    Ozon = 1,

    [Description("Wildberries")]
    Wildberries = 2,

    [Description("Яндекс.Маркет")]
    YandexMarket = 3
}
