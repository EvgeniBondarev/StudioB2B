using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Products;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValues");

        // Суррогатный PK через IBaseEntity.Id
        builder.HasKey(v => v.Id);

        // Уникальный индекс по паре (ProductId, AttributeId) — один атрибут у товара один раз
        builder.HasIndex(v => new { v.ProductId, v.AttributeId }).IsUnique();

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Attributes)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Attribute)
            .WithMany()
            .HasForeignKey(v => v.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.Value)
            .IsRequired()
            .HasMaxLength(1000);
    }
}
