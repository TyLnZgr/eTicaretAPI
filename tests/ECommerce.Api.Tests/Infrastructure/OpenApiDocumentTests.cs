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
        Assert.True(
            adjustStockResponses.TryGetProperty("401", out _));
        Assert.True(
            adjustStockResponses.TryGetProperty("403", out _));

        var stockMovementResponses = paths
            .GetProperty("/api/products/{id}/stock-movements")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(
            stockMovementResponses.TryGetProperty("200", out _));
        Assert.True(
            stockMovementResponses.TryGetProperty("404", out _));
        Assert.True(
            stockMovementResponses.TryGetProperty("401", out _));
        Assert.True(
            stockMovementResponses.TryGetProperty("403", out _));

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
        Assert.True(
            deleteCategoryResponses.TryGetProperty("401", out _));
        Assert.True(
            deleteCategoryResponses.TryGetProperty("403", out _));

        var adminOrderResponses = paths
            .GetProperty("/api/admin/orders")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(
            adminOrderResponses.TryGetProperty("200", out _));
        Assert.True(
            adminOrderResponses.TryGetProperty("400", out _));
        Assert.True(
            adminOrderResponses.TryGetProperty("401", out _));
        Assert.True(
            adminOrderResponses.TryGetProperty("403", out _));

        var cartResponses = paths
            .GetProperty("/api/cart")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(cartResponses.TryGetProperty("200", out _));
        Assert.True(cartResponses.TryGetProperty("401", out _));

        var setCartItemResponses = paths
            .GetProperty("/api/cart/items/{productId}")
            .GetProperty("put")
            .GetProperty("responses");

        Assert.True(
            setCartItemResponses.TryGetProperty("200", out _));
        Assert.True(
            setCartItemResponses.TryGetProperty("400", out _));
        Assert.True(
            setCartItemResponses.TryGetProperty("401", out _));
        Assert.True(
            setCartItemResponses.TryGetProperty("404", out _));
        Assert.True(
            setCartItemResponses.TryGetProperty("409", out _));

        var checkoutCartResponses = paths
            .GetProperty("/api/cart/checkout")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(
            checkoutCartResponses.TryGetProperty("201", out _));
        Assert.True(
            checkoutCartResponses.TryGetProperty("400", out _));
        Assert.True(
            checkoutCartResponses.TryGetProperty("401", out _));
        Assert.True(
            checkoutCartResponses.TryGetProperty("404", out _));
        Assert.True(
            checkoutCartResponses.TryGetProperty("409", out _));

        var addressCollection = paths
            .GetProperty("/api/addresses");

        var listAddressResponses = addressCollection
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(
            listAddressResponses.TryGetProperty("200", out _));
        Assert.True(
            listAddressResponses.TryGetProperty("401", out _));

        var createAddressResponses = addressCollection
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(
            createAddressResponses.TryGetProperty("201", out _));
        Assert.True(
            createAddressResponses.TryGetProperty("400", out _));
        Assert.True(
            createAddressResponses.TryGetProperty("401", out _));
        Assert.True(
            createAddressResponses.TryGetProperty("409", out _));

        var addressById = paths
            .GetProperty("/api/addresses/{id}");

        var updateAddressResponses = addressById
            .GetProperty("put")
            .GetProperty("responses");

        Assert.True(
            updateAddressResponses.TryGetProperty("200", out _));
        Assert.True(
            updateAddressResponses.TryGetProperty("400", out _));
        Assert.True(
            updateAddressResponses.TryGetProperty("401", out _));
        Assert.True(
            updateAddressResponses.TryGetProperty("404", out _));

        var deleteAddressResponses = addressById
            .GetProperty("delete")
            .GetProperty("responses");

        Assert.True(
            deleteAddressResponses.TryGetProperty("204", out _));
        Assert.True(
            deleteAddressResponses.TryGetProperty("401", out _));
        Assert.True(
            deleteAddressResponses.TryGetProperty("404", out _));

        var processPaymentResponses = paths
            .GetProperty("/api/orders/{orderId}/payments")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(
            processPaymentResponses.TryGetProperty("200", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("201", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("400", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("401", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("402", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("404", out _));
        Assert.True(
            processPaymentResponses.TryGetProperty("409", out _));

        var getPaymentResponses = paths
            .GetProperty(
                "/api/orders/{orderId}/payments/{paymentId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(
            getPaymentResponses.TryGetProperty("200", out _));
        Assert.True(
            getPaymentResponses.TryGetProperty("401", out _));
        Assert.True(
            getPaymentResponses.TryGetProperty("404", out _));
    }
}
