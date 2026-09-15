using System.Reflection;
using ECommerce.Application.Carts.Services;
using ECommerce.Domain.Carts;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Api.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DependsOnNoOtherApplicationLayer()
    {
        AssertReferencesOnly(
            typeof(Cart).Assembly,
            allowedProjectReferences: []);
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        AssertReferencesOnly(
            typeof(ICartService).Assembly,
            allowedProjectReferences: ["ECommerce.Domain"]);
    }

    [Fact]
    public void Infrastructure_DependsOnlyOnApplicationAndDomain()
    {
        AssertReferencesOnly(
            typeof(ECommerceDbContext).Assembly,
            allowedProjectReferences:
            [
                "ECommerce.Application",
                "ECommerce.Domain"
            ]);
    }

    [Fact]
    public void Api_DependsOnlyOnInnerApplicationLayers()
    {
        AssertReferencesOnly(
            typeof(global::Program).Assembly,
            allowedProjectReferences:
            [
                "ECommerce.Application",
                "ECommerce.Domain",
                "ECommerce.Infrastructure"
            ]);
    }

    private static void AssertReferencesOnly(
        Assembly assembly,
        IReadOnlyCollection<string> allowedProjectReferences)
    {
        var unexpectedReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Where(name => name!.StartsWith(
                "ECommerce.",
                StringComparison.Ordinal))
            .Except(
                allowedProjectReferences,
                StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unexpectedReferences);
    }
}
