using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Warehouses;

/// <summary>
/// Склад (собственный или Ozon). ExternalId соответствует warehouse_id из Ozon API.
/// </summary>
[Display(Name = "Склад")]
public class Warehouse : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор склада")]
    public Guid Id { get; set; }

    [Display(Name = "Склад")]
    public string Name { get; set; } = string.Empty;

    /// <summary>ID склада в Ozon (delivery_method.warehouse_id).</summary>
    [Display(Name = "Внешний ID склада (Ozon)")]
    public long? ExternalId { get; set; }

    /// <summary>Настройки 1С для склада (произвольный JSON или строка конфигурации).</summary>
    [Display(Name = "Настройки 1С")]
    public string? Settings1C { get; set; }

    public bool IsDeleted { get; set; }

    public List<WarehouseStock> Stocks { get; set; } = [];
}
