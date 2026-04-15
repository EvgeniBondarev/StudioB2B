using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class CalculationRuleCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public CalculationRuleCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static CalculationRule NewRule(string name, string formula = "1+1", int sortOrder = 1) =>
        new() { Id = Guid.NewGuid(), Name = name, ResultKey = name, Formula = formula, SortOrder = sortOrder, IsActive = true };

    [Fact]
    public async Task CreateCalculationRule_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rule = NewRule($"Rule_{Guid.NewGuid():N}");

        await ctx.CreateCalculationRuleAsync(rule);

        var loaded = await ctx.CalculationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rule.Id);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be(rule.Name);
        loaded.Formula.Should().Be(rule.Formula);
        loaded.SortOrder.Should().Be(rule.SortOrder);
    }

    [Fact]
    public async Task UpdateCalculationRule_ChangesName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rule = NewRule($"UpdRule_{Guid.NewGuid():N}");
        await ctx.CreateCalculationRuleAsync(rule);

        rule.Name = "UpdatedRuleName";
        await ctx.UpdateCalculationRuleAsync(rule);

        var loaded = await ctx.CalculationRules.AsNoTracking().FirstAsync(r => r.Id == rule.Id);
        loaded.Name.Should().Be("UpdatedRuleName");
    }

    [Fact]
    public async Task SoftDeleteCalculationRule_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rule = NewRule($"DelRule_{Guid.NewGuid():N}");
        await ctx.CreateCalculationRuleAsync(rule);

        await ctx.SoftDeleteCalculationRuleAsync(rule);

        var found = await ctx.CalculationRules.AsNoTracking().AnyAsync(r => r.Id == rule.Id);
        found.Should().BeFalse("soft-deleted rule must be excluded by global query filter");
    }

    [Fact]
    public async Task SoftDeleteCalculationRule_FoundWithIgnoreQueryFilters()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var rule = NewRule($"IgnRule_{Guid.NewGuid():N}");
        await ctx.CreateCalculationRuleAsync(rule);

        await ctx.SoftDeleteCalculationRuleAsync(rule);

        var found = await ctx.CalculationRules
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(r => r.Id == rule.Id);
        found.Should().BeTrue("deleted rule must be findable with IgnoreQueryFilters");
    }

    [Fact]
    public async Task GetActiveRules_ExcludesInactiveAndDeleted()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var activeRule = NewRule($"Active_{Guid.NewGuid():N}", "2+2", 99);
        var inactiveRule = NewRule($"Inactive_{Guid.NewGuid():N}", "3+3", 100);
        inactiveRule.IsActive = false;
        var deletedRule = NewRule($"Deleted_{Guid.NewGuid():N}", "4+4", 101);

        await ctx.CreateCalculationRuleAsync(activeRule);
        await ctx.CreateCalculationRuleAsync(inactiveRule);
        await ctx.CreateCalculationRuleAsync(deletedRule);
        await ctx.SoftDeleteCalculationRuleAsync(deletedRule);

        var result = await ctx.GetActiveRulesAsync();

        result.Should().Contain(r => r.Id == activeRule.Id, "active rule must be included");
        result.Should().NotContain(r => r.Id == inactiveRule.Id, "inactive rule must be excluded");
        result.Should().NotContain(r => r.Id == deletedRule.Id, "deleted rule must be excluded");
    }
}

