using System.Net;
using ECommerce.Api.Tests.Infrastructure;

namespace ECommerce.Api.Tests.Features.Authorization;

public sealed class CatalogAuthorizationTests
{
    public static TheoryData<string, string> ProtectedCatalogEndpoints =>
        new()
        {
            { "POST", "/api/categories" },
            { "PUT", "/api/categories/1" },
            { "DELETE", "/api/categories/1" },
            { "POST", "/api/products" },
            { "PUT", "/api/products/1" },
            { "PATCH", "/api/products/1/stock" },
            { "GET", "/api/products/1/stock-movements" },
            { "DELETE", "/api/products/1" }
        };

    [Theory]
    [InlineData("/api/categories")]
    [InlineData("/api/products")]
    public async Task PublicCatalogQuery_WithoutAuthentication_IsAccessible(
        string requestUri)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        // Act
        using var response = await client.GetAsync(requestUri);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedCatalogEndpoints))]
    public async Task ProtectedCatalogEndpoint_WithoutAuthentication_ReturnsUnauthorized(
        string method,
        string requestUri)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(
                new HttpMethod(method),
                requestUri));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedCatalogEndpoints))]
    public async Task ProtectedCatalogEndpoint_AsCustomer_ReturnsForbidden(
        string method,
        string requestUri)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(
                new HttpMethod(method),
                requestUri));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
