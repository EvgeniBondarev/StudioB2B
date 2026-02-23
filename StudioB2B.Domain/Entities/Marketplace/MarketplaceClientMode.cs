using System;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace
{
    /// <summary>
    /// Режим работы клиента на маркетплейсе: FBS/FBO/Express и т.п.
    /// </summary>
    public class MarketplaceClientMode : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}