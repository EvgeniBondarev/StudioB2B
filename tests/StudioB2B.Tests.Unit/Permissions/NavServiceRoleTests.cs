using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Web.Services;
using Xunit;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Verifies that every NavItem.Role in NavService references a valid PageEnum member name.
/// </summary>
public class NavServiceRoleTests
{
    private readonly NavService _nav = new();
    private static readonly HashSet<string> _validPageNames =
        Enum.GetNames<PageEnum>().ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void PermissionsPath_UsesPermissionsViewRole()
    {
        var item = _nav.Groups
            .SelectMany(g => g.Items)
            .FirstOrDefault(i => i.Path == "/permissions");

        item.Should().NotBeNull("NavService must have an item for /permissions");
        item.Role.Should().Be(nameof(PageEnum.PermissionsView),
            "/permissions must use PageEnum.PermissionsView, not ModulesView");
    }

    [Fact]
    public void AllNavItemRoles_ReferenceExistingPageEnum()
    {
        var items = _nav.Groups.SelectMany(g => g.Items)
            .Where(i => i.Role is not null)
            .ToList();

        items.Should().NotBeEmpty("NavService must have items with roles");

        var invalid = items
            .Where(i => !_validPageNames.Contains(i.Role!))
            .Select(i => $"Path={i.Path}, Role={i.Role}")
            .ToList();

        invalid.Should().BeEmpty(
            "all NavItem.Role values must match a PageEnum member name. Invalid: {0}",
            string.Join("; ", invalid));
    }
}
