using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Common.Observability;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Categories.Dtos;
using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Carts;
using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class OutputCacheEndpointTests
{
    [Fact]
    public async Task ProductList_CachesByQueryWithoutCachingCorrelationId()
    {
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();
        var keyboardId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Accessories",
                IsActive = true
            };

            var keyboard = CreateProduct("Keyboard", category);
            var mouse = CreateProduct("Mouse", category);

            dbContext.Products.AddRange(keyboard, mouse);
            await dbContext.SaveChangesAsync();
            keyboardId = keyboard.Id;
        });

        using var keyboardResponse = await GetWithCorrelationIdAsync(
            client,
            "/api/products?search=Keyboard",
            "cache-request-001");
        using var mouseResponse = await GetWithCorrelationIdAsync(
            client,
            "/api/products?search=Mouse",
            "cache-request-002");

        Assert.Equal(
            "Keyboard",
            Assert.Single((await ReadProductsAsync(keyboardResponse)).Items)
                .Name);
        Assert.Equal(
            "Mouse",
            Assert.Single((await ReadProductsAsync(mouseResponse)).Items)
                .Name);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            await dbContext.Products
                .Where(product => product.Id == keyboardId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    product => product.Name,
                    "Changed Keyboard"));
        });

        using var cachedResponse = await GetWithCorrelationIdAsync(
            client,
            "/api/products?search=Keyboard",
            "cache-request-003");
        var cachedProducts = await ReadProductsAsync(cachedResponse);

        Assert.Equal("Keyboard", Assert.Single(cachedProducts.Items).Name);
        Assert.Equal(
            "cache-request-003",
            Assert.Single(cachedResponse.Headers.GetValues(
                RequestCorrelationMiddleware.HeaderName)));
    }

    [Fact]
    public async Task ProductUpdate_EvictsProductListCache()
    {
        using var factory = new ECommerceApiFactory();
        using var publicClient = factory.CreateClient();
        using var adminClient = factory.CreateAdministratorClient();
        var productId = 0;
        var categoryId = 0;
        var version = 0L;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Accessories",
                IsActive = true
            };
            var product = CreateProduct("Old Keyboard", category);

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
            categoryId = category.Id;
            version = product.Version;
        });

        using var initialResponse = await publicClient.GetAsync(
            "/api/products");
        Assert.Equal(
            "Old Keyboard",
            Assert.Single((await ReadProductsAsync(initialResponse)).Items)
                .Name);

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/products/{productId}")
        {
            Content = JsonContent.Create(new UpdateProductRequest
            {
                Name = "Updated Keyboard",
                Price = 1200m,
                CategoryId = categoryId,
                IsActive = true
            })
        };
        updateRequest.Headers.TryAddWithoutValidation(
            "If-Match",
            $"\"{version}\"");

        using var updateResponse = await adminClient.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var refreshedResponse = await publicClient.GetAsync(
            "/api/products");
        var refreshedProducts = await ReadProductsAsync(refreshedResponse);

        Assert.Equal(
            "Updated Keyboard",
            Assert.Single(refreshedProducts.Items).Name);
    }

    [Fact]
    public async Task CategoryUpdate_EvictsCategoryAndProductListCaches()
    {
        using var factory = new ECommerceApiFactory();
        using var publicClient = factory.CreateClient();
        using var adminClient = factory.CreateAdministratorClient();
        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Old Category",
                IsActive = true
            };

            dbContext.Products.Add(CreateProduct("Keyboard", category));
            await dbContext.SaveChangesAsync();
            categoryId = category.Id;
        });

        using var initialCategoriesResponse = await publicClient.GetAsync(
            "/api/categories");
        using var initialProductsResponse = await publicClient.GetAsync(
            "/api/products");

        Assert.Equal(
            "Old Category",
            Assert.Single((await initialCategoriesResponse.Content
                .ReadFromJsonAsync<CategoryResponse[]>())!).Name);
        Assert.Equal(
            "Old Category",
            Assert.Single((await ReadProductsAsync(initialProductsResponse))
                .Items).CategoryName);

        using var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/categories/{categoryId}",
            new UpdateCategoryRequest
            {
                Name = "Updated Category",
                IsActive = true
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var refreshedCategoriesResponse = await publicClient.GetAsync(
            "/api/categories");
        using var refreshedProductsResponse = await publicClient.GetAsync(
            "/api/products");

        Assert.Equal(
            "Updated Category",
            Assert.Single((await refreshedCategoriesResponse.Content
                .ReadFromJsonAsync<CategoryResponse[]>())!).Name);
        Assert.Equal(
            "Updated Category",
            Assert.Single((await ReadProductsAsync(refreshedProductsResponse))
                .Items).CategoryName);
    }

    [Fact]
    public async Task OrderCreationAndCancellation_EvictProductListCache()
    {
        using var factory = new ECommerceApiFactory();
        using var publicClient = factory.CreateClient();
        var customerId = Guid.NewGuid();
        using var customerClient = factory.CreateCustomerClient(customerId);
        var productId = 0;
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));
            var address = TestEntityFactory.CreateAddress(customerId);
            var product = CreateProduct(
                "Keyboard",
                new Category
                {
                    Name = "Accessories",
                    IsActive = true
                },
                stockQuantity: 5);

            dbContext.CustomerAddresses.Add(address);
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
            addressId = address.Id;
        });

        Assert.Equal(5, await GetSingleProductStockAsync(publicClient));

        using var createOrderRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/orders")
        {
            Content = JsonContent.Create(new CreateOrderRequest
            {
                AddressId = addressId,
                Items = new List<CreateOrderItemRequest>
                {
                    new() { ProductId = productId, Quantity = 1 }
                }
            })
        };
        createOrderRequest.Headers.Add(
            "Idempotency-Key",
            "cache-order-001");

        using var createOrderResponse = await customerClient.SendAsync(
            createOrderRequest);
        Assert.Equal(HttpStatusCode.Created, createOrderResponse.StatusCode);

        var order = await createOrderResponse.Content
            .ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(4, await GetSingleProductStockAsync(publicClient));

        using var cancelResponse = await customerClient.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        Assert.Equal(5, await GetSingleProductStockAsync(publicClient));
    }

    [Fact]
    public async Task CartCheckout_EvictsProductListCache()
    {
        using var factory = new ECommerceApiFactory();
        using var publicClient = factory.CreateClient();
        var customerId = Guid.NewGuid();
        using var customerClient = factory.CreateCustomerClient(customerId);
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));
            var address = TestEntityFactory.CreateAddress(customerId);
            var product = CreateProduct(
                "Keyboard",
                new Category
                {
                    Name = "Accessories",
                    IsActive = true
                },
                stockQuantity: 5);

            dbContext.CustomerAddresses.Add(address);
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            var cart = new Cart(customerId, DateTime.UtcNow);
            cart.SetItemQuantity(
                product.Id,
                quantity: 2,
                DateTime.UtcNow);
            dbContext.Carts.Add(cart);
            await dbContext.SaveChangesAsync();

            addressId = address.Id;
        });

        Assert.Equal(5, await GetSingleProductStockAsync(publicClient));

        using var checkoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/cart/checkout")
        {
            Content = JsonContent.Create(
                new CheckoutCartRequest { AddressId = addressId })
        };
        checkoutRequest.Headers.Add(
            "Idempotency-Key",
            "cache-checkout-001");

        using var checkoutResponse = await customerClient.SendAsync(
            checkoutRequest);

        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        Assert.Equal(3, await GetSingleProductStockAsync(publicClient));
    }

    private static Product CreateProduct(
        string name,
        Category category,
        int stockQuantity = 10)
    {
        return new Product
        {
            Name = name,
            Price = 1000m,
            StockQuantity = stockQuantity,
            IsActive = true,
            Category = category
        };
    }

    private static async Task<PagedResult<ProductResponse>> ReadProductsAsync(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return (await response.Content
            .ReadFromJsonAsync<PagedResult<ProductResponse>>())!;
    }

    private static async Task<int> GetSingleProductStockAsync(
        HttpClient client)
    {
        using var response = await client.GetAsync("/api/products");
        var products = await ReadProductsAsync(response);
        return Assert.Single(products.Items).StockQuantity;
    }

    private static async Task<HttpResponseMessage>
        GetWithCorrelationIdAsync(
            HttpClient client,
            string requestUri,
            string correlationId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            requestUri);
        request.Headers.Add(
            RequestCorrelationMiddleware.HeaderName,
            correlationId);

        return await client.SendAsync(request);
    }
}
