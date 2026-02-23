using System;
using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Web.Components.Common
{
    public class MarketplaceClientFormModel
    {
        public Guid? Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? ApiId { get; set; }

        public string? Key { get; set; }

        public Guid? ClientTypeId { get; set; }
        public Guid? ModeId { get; set; }
    }
}