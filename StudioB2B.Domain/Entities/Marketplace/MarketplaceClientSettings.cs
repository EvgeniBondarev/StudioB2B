using System;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace
{
    /// <summary>
    /// Ключ‑значение‑настройки клиента. Сейчас хранится в отдельной таблице,
    /// но можно заменить на json‑поле, если требуется.
    /// </summary>
    public class MarketplaceClientSettings : BaseEntity
    {
        public Guid MarketplaceClientId { get; set; }
        public MarketplaceClient MarketplaceClient { get; set; } = null!;

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}