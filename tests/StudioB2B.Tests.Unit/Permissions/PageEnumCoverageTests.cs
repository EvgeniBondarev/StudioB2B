using FluentAssertions;
using StudioB2B.Domain.Constants;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every PageEnum value is referenced in at least one Authorize attribute
/// or AuthorizeView in the Web project source files.
/// </summary>
public class PageEnumCoverageTests : RazorCoverageBase
{
    private static readonly string _allSource = GetAllSourceText();

    public static TheoryData<PageEnum> AllPages
    {
        get
        {
            var data = new TheoryData<PageEnum>();
            foreach (var v in Enum.GetValues<PageEnum>())
                data.Add(v);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllPages))]
    public void PageEnum_IsReferencedInAuthorize(PageEnum page)
    {
        var memberName = page.ToString();
        var found = _allSource.Contains($"nameof(PageEnum.{memberName})")
                 || _allSource.Contains($"PageEnum.{memberName}");

        found.Should().BeTrue(
            $"PageEnum.{memberName} must be referenced in an [Authorize] attribute or AuthorizeView in the Web project");
    }
}
