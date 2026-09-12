using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class OpenApiDocumentTests
{
    [Fact]
    public async Task OpenApiDocument_DescribesSuccessAndProblemResponses()
    {
        // Arrange
        using var factory =
            new ECommerceApiFactory(Environments.Development);
        using var client = factory.CreateClient();

        // Act
        using var response =
            await client.GetAsync("/openapi/v1.json");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        await using var responseStream =
            await response.Content.ReadAsStreamAsync();
        using var document =
            await JsonDocument.ParseAsync(responseStream);

        var root = document.RootElement;
        var openApiVersion = root
            .GetProperty("openapi")
            .GetString();

        Assert.NotNull(openApiVersion);
        Assert.StartsWith("3.", openApiVersion);

        var paths = root.GetProperty("paths");

        var getProducts = paths
            .GetProperty("/api/products")
            .GetProperty("get");

        Assert.Equal(
            "GetProducts",
            getProducts.GetProperty("operationId").GetString());
        Assert.Equal(
            "List products",
            getProducts.GetProperty("summary").GetString());

        var productResponses =
            getProducts.GetProperty("responses");

        Assert.True(productResponses.TryGetProperty("200", out _));
        Assert.True(
            productResponses.TryGetProperty(
                "400",
                out var validationResponse));

        var validationContent =
            validationResponse.GetProperty("content");

        Assert.True(
            validationContent.TryGetProperty(
                "application/problem+json",
                out _));

        var adjustStock = paths
            .GetProperty("/api/products/{id}/stock")
            .GetProperty("patch");

        Assert.Equal(
            "AdjustProductStock",
            adjustStock.GetProperty("operationId").GetString());

        var adjustStockResponses =
            adjustStock.GetProperty("responses");

        Assert.True(
            adjustStockResponses.TryGetProperty("204", out _));
        Assert.True(
            adjustStockResponses.TryGetProperty("400", out _));
        Assert.True(
            adjustStockResponses.TryGetProperty("404", out _));
        Assert.True(
            adjustStockResponses.TryGetProperty("409", out _));

        var stockMovementResponses = paths
            .GetProperty("/api/products/{id}/stock-movements")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(
            stockMovementResponses.TryGetProperty("200", out _));
        Assert.True(
            stockMovementResponses.TryGetProperty("404", out _));

        var deleteCategoryResponses = paths
            .GetProperty("/api/categories/{id}")
            .GetProperty("delete")
            .GetProperty("responses");

        Assert.True(
            deleteCategoryResponses.TryGetProperty("204", out _));
        Assert.True(
            deleteCategoryResponses.TryGetProperty("404", out _));
        Assert.True(
            deleteCategoryResponses.TryGetProperty("409", out _));
    }
}
