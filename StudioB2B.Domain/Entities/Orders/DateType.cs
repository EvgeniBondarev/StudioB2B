using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Тип даты в отправлении (например «дата отгрузки», «дата доставки», «дата создания»).
/// </summary>
[Display(Name = "Тип даты")]
public class DateType : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор типа даты")]
    public Guid Id { get; set; }

    [Display(Name = "Тип даты")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
