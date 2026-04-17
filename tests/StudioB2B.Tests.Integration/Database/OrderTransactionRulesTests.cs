using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет создание документа заказа с правилами цен и полей,
/// обновление (полная замена правил) и загрузку для редактирования.
/// </summary>
[Collection("Database")]
public class OrderTransactionRulesTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderTransactionRulesTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private SaveOrderTransactionRequest RequestWithRules(string name, Guid priceTypeId) =>
        new(
            name,
            _fixture.DefaultFromStatusId,
            _fixture.DefaultToStatusId,
            IsEnabled: true,
            Color: "#FF0000",
            Icon: "task_alt",
            Rules: [new SaveTransactionRuleRequest(
                priceTypeId,
                TransactionValueSourceEnum.UserInput,
                Formula: null,
                ProductId: null,
                IsRequired: true)],
            FieldRules: [new SaveTransactionFieldRuleRequest(
                "Shipment.TrackingNumber",
                TransactionFieldValueSourceEnum.Fixed,
                FixedValue: "TRACK-001",
                IsRequired: false)]);

    [Fact]
    public async Task CreateTransaction_WithRulesAndFieldRules_PersistsBoth()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt = DatabaseSeeder.PriceType();
        ctx.PriceTypes.Add(pt);
        await ctx.SaveChangesAsync();

        var t = await ctx.CreateOrderTransactionAsync(RequestWithRules($"T_{Guid.NewGuid():N}", pt.Id));

        var loaded = await ctx.GetOrderTransactionForEditAsync(t.Id);
        loaded.Should().NotBeNull();
        loaded!.Rules.Should().HaveCount(1);
        loaded.Rules[0].PriceTypeId.Should().Be(pt.Id);
        loaded.Rules[0].IsRequired.Should().BeTrue();
        loaded.FieldRules.Should().HaveCount(1);
        loaded.FieldRules[0].EntityPath.Should().Be("Shipment.TrackingNumber");
        loaded.FieldRules[0].FixedValue.Should().Be("TRACK-001");
    }

    [Fact]
    public async Task UpdateTransaction_ReplacesRules()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt1 = DatabaseSeeder.PriceType();
        var pt2 = DatabaseSeeder.PriceType();
        ctx.PriceTypes.AddRange(pt1, pt2);
        await ctx.SaveChangesAsync();

        var t = await ctx.CreateOrderTransactionAsync(RequestWithRules($"T_{Guid.NewGuid():N}", pt1.Id));

        // Replace with pt2 rule, no field rules
        var updateRequest = new SaveOrderTransactionRequest(
            t.Name,
            _fixture.DefaultFromStatusId,
            _fixture.DefaultToStatusId,
            IsEnabled: true,
            Color: null,
            Icon: null,
            Rules: [new SaveTransactionRuleRequest(pt2.Id, TransactionValueSourceEnum.UserInput, null, null, false)],
            FieldRules: []);

        var ok = await ctx.UpdateOrderTransactionAsync(t.Id, updateRequest);
        ok.Should().BeTrue();

        var loaded = await ctx.GetOrderTransactionForEditAsync(t.Id);
        loaded!.Rules.Should().HaveCount(1);
        loaded.Rules[0].PriceTypeId.Should().Be(pt2.Id);
        loaded.FieldRules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEnabledTransactionsWithStatuses_ExcludesDisabled()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var enabledReq = new SaveOrderTransactionRequest(
            $"Enabled_{Guid.NewGuid():N}",
            _fixture.DefaultFromStatusId, _fixture.DefaultToStatusId,
            IsEnabled: true, null, null, [], []);
        var disabledReq = new SaveOrderTransactionRequest(
            $"Disabled_{Guid.NewGuid():N}",
            _fixture.DefaultFromStatusId, _fixture.DefaultToStatusId,
            IsEnabled: false, null, null, [], []);

        var enabled = await ctx.CreateOrderTransactionAsync(enabledReq);
        var disabled = await ctx.CreateOrderTransactionAsync(disabledReq);

        var result = await ctx.GetEnabledOrderTransactionsWithStatusesAsync();

        result.Should().Contain(x => x.Id == enabled.Id);
        result.Should().NotContain(x => x.Id == disabled.Id);
    }

    [Fact]
    public async Task GetCanvasData_ReturnsBothStatusesAndTransactions()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var (statuses, transactions) = await ctx.GetCanvasDataAsync();

        statuses.Should().Contain(s => s.Id == _fixture.DefaultFromStatusId);
        statuses.Should().Contain(s => s.Id == _fixture.DefaultToStatusId);
        transactions.Should().NotBeNull();
    }
}

