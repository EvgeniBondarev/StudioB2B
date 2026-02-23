using System;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace
{
    /// <summary>
    /// Дополнительные поля, которые нужны для обмена с 1С.
    /// </summary>
    public class MarketplaceClient1CSettings : BaseEntity
    {
        public Guid MarketplaceClientId { get; set; }
        public MarketplaceClient MarketplaceClient { get; set; } = null!;

        public string? INN { get; set; }
        public string? Country { get; set; }
        public string? Currency { get; set; }
        public string? LegalName { get; set; }
        public string? OzonName { get; set; }
        public string? OGRN { get; set; }
        public string? OwnershipForm { get; set; }

        // можно расширить дополнительными полями по необходимости
    }
}