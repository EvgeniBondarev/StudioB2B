using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет CRUD для ставок оплаты коммуникационных задач.
/// </summary>
[Collection("Database")]
public class CommunicationPaymentRateCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public CommunicationPaymentRateCrudTests(TenantDbContextFixture fixture)
        => _fixture = fixture;

    private static CommunicationPaymentRate NewRate(
        CommunicationTaskType? taskType = null,
        decimal rate = 100m,
        bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            TaskType = taskType,
            PaymentMode = PaymentMode.PerTask,
            Rate = rate,
            IsActive = isActive
        };

    [Fact]
    public async Task CreatePaymentRate_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rate = NewRate(CommunicationTaskType.Chat, rate: 150m);
        ctx.CommunicationPaymentRates.Add(rate);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationPaymentRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rate.Id);

        loaded.Should().NotBeNull();
        loaded!.Rate.Should().Be(150m);
        loaded.TaskType.Should().Be(CommunicationTaskType.Chat);
        loaded.PaymentMode.Should().Be(PaymentMode.PerTask);
    }

    [Fact]
    public async Task UpdatePaymentRate_ChangesRate_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rate = NewRate(rate: 100m);
        ctx.CommunicationPaymentRates.Add(rate);
        await ctx.SaveChangesAsync();

        var entity = await ctx.CommunicationPaymentRates.FindAsync(rate.Id);
        entity!.Rate = 200m;
        entity.MinDurationMinutes = 30;
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationPaymentRates
            .AsNoTracking()
            .FirstAsync(r => r.Id == rate.Id);

        loaded.Rate.Should().Be(200m);
        loaded.MinDurationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task DeactivatePaymentRate_IsActiveSetToFalse()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rate = NewRate(isActive: true);
        ctx.CommunicationPaymentRates.Add(rate);
        await ctx.SaveChangesAsync();

        var entity = await ctx.CommunicationPaymentRates.FindAsync(rate.Id);
        entity!.IsActive = false;
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationPaymentRates
            .AsNoTracking()
            .FirstAsync(r => r.Id == rate.Id);
        loaded.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePaymentRate_RemovedFromDatabase()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rate = NewRate();
        ctx.CommunicationPaymentRates.Add(rate);
        await ctx.SaveChangesAsync();

        var entity = await ctx.CommunicationPaymentRates.FindAsync(rate.Id);
        ctx.CommunicationPaymentRates.Remove(entity!);
        await ctx.SaveChangesAsync();

        var found = await ctx.CommunicationPaymentRates
            .AsNoTracking()
            .AnyAsync(r => r.Id == rate.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task NullTaskType_Rate_AppliesToAllTypes()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        // null TaskType = универсальная ставка для всех типов задач
        var globalRate = NewRate(taskType: null, rate: 50m);
        ctx.CommunicationPaymentRates.Add(globalRate);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationPaymentRates
            .AsNoTracking()
            .FirstAsync(r => r.Id == globalRate.Id);

        loaded.TaskType.Should().BeNull();
        loaded.Rate.Should().Be(50m);
    }
}

