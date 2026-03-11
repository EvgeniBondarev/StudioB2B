using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Схема доставки маркетплейса: FBS, FBO, Express.
/// </summary>
[Display(Name = "Тип доставки")]
public class DeliveryType : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор схемы доставки")]
    public Guid Id { get; set; }

    /// <summary>Код схемы: fbs / fbo / express.</summary>
    [Display(Name = "Схема доставки")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
