using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Информация о складах в рамках отправления: склад-получатель и склад-отправитель.
/// </summary>
[Display(Name = "Информация о складе")]
public class WarehouseInfo : IBaseEntity
{
    [Display(Name = "Идентификатор связки складов")]
    public Guid Id { get; set; }

    /// <summary>Склад назначения (получатель).</summary>
    [Display(Name = "Склад-получатель")]
    public Guid? RecipientWarehouseId { get; set; }
    public Warehouse? RecipientWarehouse { get; set; }

    /// <summary>Склад отправки (отправитель).</summary>
    [Display(Name = "Склад-отправитель")]
    public Guid? SenderWarehouseId { get; set; }
    public Warehouse? SenderWarehouse { get; set; }
}
