using FluentAssertions;
using StudioB2B.Domain.Constants;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every FunctionEnum value is used somewhere in the Web project
/// (in IsInRole, AuthorizeView Roles, or similar).
/// If a function is never checked, the permission entry is dead code.
/// </summary>
public class FunctionEnumCoverageTests : RazorCoverageBase
{
    private static readonly string _allSource = GetAllSourceText();

    public static TheoryData<FunctionEnum> AllFunctions
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
    [MemberData(nameof(AllFunctions))]
    public void FunctionEnum_IsReferencedInWebProject(FunctionEnum func)
    {
        var memberName = func.ToString();
        var found = _allSource.Contains($"nameof(FunctionEnum.{memberName})")
                 || _allSource.Contains($"FunctionEnum.{memberName}");

        found.Should().BeTrue(
            $"FunctionEnum.{memberName} must be referenced in the Web project " +
            $"(in IsInRole, AuthorizeView Roles, etc.). " +
            $"If it is unused, consider removing it from the enum.");
    }
}
