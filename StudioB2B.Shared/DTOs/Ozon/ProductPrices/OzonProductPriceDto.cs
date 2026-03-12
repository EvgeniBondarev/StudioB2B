using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Блок price из ответа /v5/product/info/prices.
/// Все денежные поля соответствуют числовым значениям в JSON.
/// </summary>
public class OzonProductPriceDto
{
    [JsonPropertyName("auto_action_enabled")]
    public bool AutoActionEnabled { get; set; }

    [JsonPropertyName("auto_add_to_ozon_actions_list_enabled")]
    public bool AutoAddToOzonActionsListEnabled { get; set; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    /// <summary>Цена товара с учётом акций продавца.</summary>
    [JsonPropertyName("marketing_seller_price")]
    public decimal? MarketingSellerPrice { get; set; }

    /// <summary>Минимальная цена товара после применения всех скидок.</summary>
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    /// <summary>Себестоимость товара.</summary>
    [JsonPropertyName("net_price")]
    public decimal? NetPrice { get; set; }

    /// <summary>Цена до учёта скидок (отображается зачёркнутой).</summary>
    [JsonPropertyName("old_price")]
    public decimal? OldPrice { get; set; }

    /// <summary>Цена товара с учётом скидок (отображается на карточке).</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>Цена поставщика по договору.</summary>
    [JsonPropertyName("retail_price")]
    public decimal? RetailPrice { get; set; }

    /// <summary>Ставка НДС.</summary>
    [JsonPropertyName("vat")]
    public double? Vat { get; set; }
}

