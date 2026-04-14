using System.Reflection;
using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every FunctionEnum value is present in FunctionPageMap
/// inside TenantDatabaseInitializer. Missing entries won't be seeded and
/// won't appear in the Permissions UI.
/// </summary>
public class FunctionEnumMapTests
{
    private static Dictionary<FunctionEnum, PageEnum> GetFunctionPageMap()
    {
        var field = typeof(TenantDatabaseInitializer)
            .GetField("FunctionPageMap", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("FunctionPageMap field must exist in TenantDatabaseInitializer");
        return (Dictionary<FunctionEnum, PageEnum>)field!.GetValue(null)!;
    }

    public static TheoryData<FunctionEnum> AllFunctionEnumValues
    {
        get
        {
            var data = new TheoryData<FunctionEnum>();
            foreach (var v in Enum.GetValues<FunctionEnum>())
                data.Add(v);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllFunctionEnumValues))]
    public void FunctionEnum_IsInFunctionPageMap(FunctionEnum value)
    {
        var map = GetFunctionPageMap();
        map.Should().ContainKey(value,
            $"FunctionEnum.{value} must be registered in FunctionPageMap in TenantDatabaseInitializer");
    }
}
