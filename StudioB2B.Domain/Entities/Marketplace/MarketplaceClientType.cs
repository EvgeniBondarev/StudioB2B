using System;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace
{
    /// <summary>
    /// Справочник «Тип клиента» (позволяет хранить дополнительные классификации).
    /// </summary>
    public class MarketplaceClientType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}