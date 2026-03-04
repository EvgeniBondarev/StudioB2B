using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Products;

namespace StudioB2B.Domain.Entities.Warehouses;

/// <summary>
/// Остатки товара на складе.
/// </summary>
[Display(Name = "Остаток на складе")]
public class WarehouseStock : IBaseEntity
{
    [Display(Name = "Идентификатор остатка")]
    public Guid Id { get; set; }

    [Display(Name = "Склад")]
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    [Display(Name = "Товар")]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Display(Name = "Количество")]
    public int Quantity { get; set; }

    [Display(Name = "Дата обновления")]
    public DateTime UpdatedAt { get; set; }
}
