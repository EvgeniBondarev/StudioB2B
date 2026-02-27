using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Схема доставки маркетплейса: FBS, FBO, Express.
/// </summary>
public class DeliveryType : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор схемы доставки")]
    public Guid Id { get; set; }

    /// <summary>Код схемы: fbs / fbo / express.</summary>
    [Display(Name = "Схема доставки")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
