using System.Reflection;
using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every PageColumnEnum value is present in ColumnPageMap
/// inside TenantDatabaseInitializer.
/// </summary>
public class PageColumnEnumMapTests
{
    private static Dictionary<PageColumnEnum, PageEnum> GetColumnPageMap()
    {
        var field = typeof(TenantDatabaseInitializer)
            .GetField("ColumnPageMap", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("ColumnPageMap field must exist in TenantDatabaseInitializer");
        return (Dictionary<PageColumnEnum, PageEnum>)field!.GetValue(null)!;
    }

    public static TheoryData<PageColumnEnum> AllColumnEnumValues
    {
        get
        {
            var data = new TheoryData<PageColumnEnum>();
            foreach (var v in Enum.GetValues<PageColumnEnum>())
                data.Add(v);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllColumnEnumValues))]
    public void PageColumnEnum_IsInColumnPageMap(PageColumnEnum value)
    {
        var map = GetColumnPageMap();
        map.Should().ContainKey(value,
            $"PageColumnEnum.{value} must be registered in ColumnPageMap in TenantDatabaseInitializer");
    }
}
