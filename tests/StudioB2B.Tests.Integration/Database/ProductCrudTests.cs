using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class ProductCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public ProductCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateProduct_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var product = DatabaseSeeder.Product();
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be(product.Name);
        loaded.Article.Should().Be(product.Article);
    }

    [Fact]
    public async Task CreateProduct_WithManufacturer_NavigationLoads()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var mfr = DatabaseSeeder.Manufacturer();
        ctx.Manufacturers.Add(mfr);
        await ctx.SaveChangesAsync();

        var product = DatabaseSeeder.Product();
        product.ManufacturerId = mfr.Id;
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Products
            .Include(p => p.Manufacturer)
            .AsNoTracking()
            .FirstAsync(p => p.Id == product.Id);

        loaded.Manufacturer.Should().NotBeNull();
        loaded.Manufacturer!.Name.Should().Be(mfr.Name);
    }

    [Fact]
    public async Task SoftDeleteProduct_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var product = DatabaseSeeder.Product();
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var entity = await ctx.Products.FindAsync(product.Id);
        entity!.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var found = await ctx.Products.AsNoTracking().AnyAsync(p => p.Id == product.Id);
        found.Should().BeFalse("soft-deleted product must be excluded");

        var foundIgnored = await ctx.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.Id == product.Id);
        foundIgnored.Should().BeTrue("must exist with IgnoreQueryFilters");
    }

    [Fact]
    public async Task AddProductAttributeValue_PersistsAndLoads()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var attr = new ProductAttribute
        {
            Id = Guid.NewGuid(),
            Name = $"Attr_{Guid.NewGuid():N}"
        };
        ctx.ProductAttributes.Add(attr);

        var product = DatabaseSeeder.Product();
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var attrValue = new ProductAttributeValue
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            AttributeId = attr.Id,
            Value = "TestValue"
        };
        ctx.ProductAttributeValues.Add(attrValue);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Products
            .Include(p => p.Attributes).ThenInclude(a => a.Attribute)
            .AsNoTracking()
            .FirstAsync(p => p.Id == product.Id);

        loaded.Attributes.Should().HaveCount(1);
        loaded.Attributes[0].Value.Should().Be("TestValue");
        loaded.Attributes[0].Attribute.Name.Should().Be(attr.Name);
    }
}
