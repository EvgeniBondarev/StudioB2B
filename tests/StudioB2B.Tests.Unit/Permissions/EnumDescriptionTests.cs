using System.ComponentModel;
using System.Reflection;
using FluentAssertions;
using StudioB2B.Domain.Constants;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every enum member in the permission system has a non-empty [Description] attribute.
/// Missing descriptions appear as blank entries in the Permissions UI.
/// </summary>
public class EnumDescriptionTests
{
    private static TheoryData<Enum> GetEnumValues<T>() where T : Enum
    {
        var data = new TheoryData<Enum>();
        foreach (var v in Enum.GetValues(typeof(T)).Cast<Enum>())
            data.Add(v);
        return data;
    }

    public static TheoryData<Enum> PageEnumValues => GetEnumValues<PageEnum>();
    public static TheoryData<Enum> FunctionEnumValues => GetEnumValues<FunctionEnum>();
    public static TheoryData<Enum> PageColumnEnumValues => GetEnumValues<PageColumnEnum>();

    [Theory]
    [MemberData(nameof(PageEnumValues))]
    public void PageEnum_HasDescription(Enum value) => AssertHasDescription(value);

    [Theory]
    [MemberData(nameof(FunctionEnumValues))]
    public void FunctionEnum_HasDescription(Enum value) => AssertHasDescription(value);

    [Theory]
    [MemberData(nameof(PageColumnEnumValues))]
    public void PageColumnEnum_HasDescription(Enum value) => AssertHasDescription(value);

    private static void AssertHasDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString())!;
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        attr.Should().NotBeNull($"{value.GetType().Name}.{value} must have [Description]");
        attr!.Description.Should().NotBeNullOrWhiteSpace(
            $"{value.GetType().Name}.{value} description must not be empty");
    }
}
