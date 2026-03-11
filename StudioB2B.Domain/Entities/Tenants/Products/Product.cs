using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Товар из каталога маркетплейса (Ozon: product).
/// </summary>
[Display(Name = "Товар")]
public class Product : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор товара")]
    public Guid Id { get; set; }

    /// <summary>Артикул продавца (offer_id в Ozon).</summary>
    [Display(Name = "Артикул (offer_id)")]
    public string? Article { get; set; }

    /// <summary>SKU товара в Ozon.</summary>
    [Display(Name = "SKU (Ozon)")]
    public long? Sku { get; set; }

    [Display(Name = "Название товара")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    /// <summary>URL изображения.</summary>
    [Display(Name = "Изображение (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Штрихкод")]
    public string? Barcode { get; set; }

    /// <summary>Идентификатор связи (link_id в Ozon — связь FBS/FBO SKU).</summary>
    [Display(Name = "LinkId (связь FBS/FBO)")]
    public string? LinkId { get; set; }

    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool IsDeleted { get; set; }

    public List<ProductAttributeValue> Attributes { get; set; } = [];
}
