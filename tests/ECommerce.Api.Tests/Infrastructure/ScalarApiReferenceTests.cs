using System.Net;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class ScalarApiReferenceTests
{
    [Fact]
    public async Task ScalarReference_InDevelopment_ReturnsHtmlPage()
    {
        // Arrange
        using var factory =
            new ECommerceApiFactory(Environments.Development);
        using var client = factory.CreateClient();

        // Act
        using var response =
            await client.GetAsync("/scalar");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "text/html",
            response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("ECommerce API", html);
    }

    [Fact]
    public async Task ScalarReference_InTesting_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response =
            await client.GetAsync("/scalar");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}
