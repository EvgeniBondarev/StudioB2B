using FluentAssertions;
using StudioB2B.Domain.Constants;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every PageColumnEnum value is used in a Col(...) call in the Web project.
/// Unused column permissions are dead entries in the Permissions UI.
/// </summary>
public class PageColumnEnumCoverageTests : RazorCoverageBase
{
    private static readonly string _allSource = GetAllSourceText();

    public static TheoryData<PageColumnEnum> AllColumns
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
    [MemberData(nameof(AllColumns))]
    public void PageColumnEnum_IsUsedInColCall(PageColumnEnum col)
    {
        var memberName = col.ToString();
        var found = _allSource.Contains($"nameof(PageColumnEnum.{memberName})")
                 || _allSource.Contains($"PageColumnEnum.{memberName}");

        found.Should().BeTrue(
            $"PageColumnEnum.{memberName} must be referenced in Col(...) or similar in the Web project");
    }
}
