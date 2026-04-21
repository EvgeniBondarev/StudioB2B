using FluentAssertions;
using NetArchTest.Rules;
using StudioB2B.Infrastructure;
using StudioB2B.Web.Components;
using Xunit;

namespace StudioB2B.Tests.Unit.Architecture;

/// <summary>
/// Enforces the layered architecture rules defined in copilot-instructions.md.
/// UI → Service → Feature → Database. No layer should skip a level.
/// </summary>
public class ArchitectureTests
{
    private static Types WebTypes()
        => Types.InAssembly(typeof(App).Assembly);

    private static Types InfraTypes()
        => Types.InAssembly(typeof(DependencyInjection).Assembly);

    [Fact]
    public void BlazorPages_DoNotDirectlyUse_ITenantDbContextFactory()
    {
        var result = WebTypes()
            .That().ResideInNamespace("StudioB2B.Web.Components.Pages")
            .ShouldNot().HaveDependencyOn("StudioB2B.Infrastructure.Interfaces.ITenantDbContextFactory")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Blazor pages must use Service layer, not inject ITenantDbContextFactory directly. " +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BlazorPages_DoNotDirectlyUse_IMapper()
    {
        var result = WebTypes()
            .That().ResideInNamespace("StudioB2B.Web.Components.Pages")
            .ShouldNot().HaveDependencyOn("AutoMapper.IMapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Blazor pages must not inject IMapper directly. " +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ServiceLayer_DoesNotDependOn_WebProject()
    {
        var result = InfraTypes()
            .That().ResideInNamespace("StudioB2B.Infrastructure.Services")
            .ShouldNot().HaveDependencyOn("StudioB2B.Web")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure.Services must not reference the Web project. " +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void FeatureLayer_ContainsOnlyStaticClasses()
    {
        var result = InfraTypes()
            .That().ResideInNamespace("StudioB2B.Infrastructure.Features")
            .And().AreNotAbstract()
            .Should().BeStatic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Feature layer classes must be static (extension method containers). " +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
