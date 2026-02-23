using System;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace
{
    /// <summary>
    /// Клиент маркетплейса (Ozon, Wildberries и т.п.)
    /// Нормализованное представление: основные параметры + ссылки на вспомогательные
    /// сущности.
    /// </summary>
    public class MarketplaceClient : BaseEntity
    {
        /// <summary>название компании (внутри системы)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>идентификатор для API маркетплейса</summary>
        public string ApiId { get; set; } = string.Empty;

        /// <summary>ключ доступа (обычно API‑secret)</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>тип клиента (для классификации: торговый, логистический и т.п.)</summary>
        public Guid? ClientTypeId { get; set; }
        public MarketplaceClientType? ClientType { get; set; }

        /// <summary>режим продавца на маркетплейсе (FBS/FBO/Express).</summary>
        public Guid? ModeId { get; set; }
        public MarketplaceClientMode? Mode { get; set; }

        /// <summary>общие настройки пользователя; произвольные ключ‑значение.</summary>
        // navigation for key‑value settings; multiple entries allowed
        public List<MarketplaceClientSettings> Settings { get; set; } = new();

        /// <summary>настройки, специфичные для интеграции с 1С.</summary>
        public MarketplaceClient1CSettings? Settings1C { get; set; }

        // расширяемая часть данных, пришедшая из Ozon (или другой площадки)
        // хранится как JSON, но в домене можно создать отдельную структуру
        public string? Company { get; set; }

        // базовые параметры, дублируемые из Settings1C для быстрого доступа
        public string? Country { get; set; }
        public string? Currency { get; set; }
        public string? INN { get; set; }
        public string? LegalName { get; set; }
        public string? OzonName { get; set; }
        public string? OGRN { get; set; }
        public string? OwnershipForm { get; set; }

    }
}