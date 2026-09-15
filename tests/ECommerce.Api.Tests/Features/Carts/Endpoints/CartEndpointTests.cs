using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Features.Carts.Dtos;
using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Domain.Carts;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Orders;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Carts.Endpoints;

public sealed class CartEndpointTests
{
    public static TheoryData<string, string> ProtectedCartEndpoints =>
        new()
        {
            { "GET", "/api/cart" },
            { "PUT", "/api/cart/items/1" },
            { "DELETE", "/api/cart/items/1" },
            { "DELETE", "/api/cart" },
            { "POST", "/api/cart/checkout" }
        };

    [Theory]
    [MemberData(nameof(ProtectedCartEndpoints))]
    public async Task CartEndpoint_WithoutAuthentication_ReturnsUnauthorized(
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

    [Fact]
    public async Task GetAsync_WhenCartDoesNotExist_ReturnsEmptyCartWithoutCreatingRow()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        // Act
        using var response = await client.GetAsync("/api/cart");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await response.Content
            .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(cart);
        Assert.Equal(0, cart.TotalQuantity);
        Assert.Equal(0m, cart.TotalAmount);
        Assert.Null(cart.UpdatedAtUtc);
        Assert.Empty(cart.Items);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.False(await dbContext.Carts.AnyAsync());
        });
    }

    [Fact]
    public async Task SetItemQuantityAsync_WhenValid_ReturnsCurrentTotalsWithoutChangingStock()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(
            customerId,
            "customer@example.com");

        var productId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var product = CreateProduct(
                "Keyboard",
                price: 1250m,
                stockQuantity: 10);

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        });

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/cart/items/{productId}",
            new SetCartItemQuantityRequest
            {
                Quantity = 2
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await response.Content
            .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(cart);
        Assert.Equal(2, cart.TotalQuantity);
        Assert.Equal(2500m, cart.TotalAmount);

        var item = Assert.Single(cart.Items);

        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Keyboard", item.ProductName);
        Assert.Equal(1250m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(2500m, item.LineTotal);
        Assert.Equal(10, item.AvailableStock);
        Assert.True(item.IsAvailable);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var stockQuantity = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.StockQuantity)
                .SingleAsync();

            Assert.Equal(10, stockQuantity);
        });
    }

    [Fact]
    public async Task GetAsync_WithDifferentCustomers_ReturnsOnlyOwnedCart()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        using var firstClient = factory.CreateCustomerClient(
            firstCustomerId,
            "first@example.com");
        using var secondClient = factory.CreateCustomerClient(
            secondCustomerId,
            "second@example.com");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var firstProduct = CreateProduct("First Product");
            var secondProduct = CreateProduct("Second Product");

            dbContext.Users.AddRange(
                TestEntityFactory.CreateUser(
                    firstCustomerId,
                    "first@example.com"),
                TestEntityFactory.CreateUser(
                    secondCustomerId,
                    "second@example.com"));

            dbContext.Carts.AddRange(
                CreateCart(firstCustomerId, firstProduct),
                CreateCart(secondCustomerId, secondProduct));

            await dbContext.SaveChangesAsync();
        });

        // Act
        var firstCart = await firstClient.GetFromJsonAsync<CartResponse>(
            "/api/cart");
        var secondCart = await secondClient.GetFromJsonAsync<CartResponse>(
            "/api/cart");

        // Assert
        Assert.NotNull(firstCart);
        Assert.NotNull(secondCart);
        Assert.Equal(
            "First Product",
            Assert.Single(firstCart.Items).ProductName);
        Assert.Equal(
            "Second Product",
            Assert.Single(secondCart.Items).ProductName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task SetItemQuantityAsync_WhenQuantityIsInvalid_ReturnsValidationProblem(
        int quantity)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.PutAsJsonAsync(
            "/api/cart/items/1",
            new SetCartItemQuantityRequest
            {
                Quantity = quantity
            });

        // Assert
        await ProblemDetailsAssertions.AssertValidationAsync(
            response,
            "quantity",
            "Quantity must be between 1 and 1000.");
    }

    [Fact]
    public async Task SetItemQuantityAsync_WhenProductIsInactive_ReturnsConflict()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var productId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var product = CreateProduct("Inactive Product");
            product.IsActive = false;

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        });

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/cart/items/{productId}",
            new SetCartItemQuantityRequest
            {
                Quantity = 1
            });

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            $"Product with ID {productId} is not active.");
    }

    [Fact]
    public async Task SetItemQuantityAsync_WhenStockIsInsufficient_ReturnsAvailableStock()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var productId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var product = CreateProduct(
                "Low Stock Product",
                stockQuantity: 1);

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        });

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/cart/items/{productId}",
            new SetCartItemQuantityRequest
            {
                Quantity = 2
            });

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            $"Product with ID {productId} has only 1 units available.");
    }

    [Fact]
    public async Task RemoveAndClearAsync_WhenItemsExist_ProduceEmptyCart()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var firstProductId = 0;
        var secondProductId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var firstProduct = CreateProduct("Keyboard");
            var secondProduct = CreateProduct("Mouse");

            dbContext.Products.AddRange(
                firstProduct,
                secondProduct);
            await dbContext.SaveChangesAsync();

            firstProductId = firstProduct.Id;
            secondProductId = secondProduct.Id;
        });

        await client.PutAsJsonAsync(
            $"/api/cart/items/{firstProductId}",
            new SetCartItemQuantityRequest { Quantity = 1 });
        await client.PutAsJsonAsync(
            $"/api/cart/items/{secondProductId}",
            new SetCartItemQuantityRequest { Quantity = 2 });

        // Act - remove one item
        using var removeResponse = await client.DeleteAsync(
            $"/api/cart/items/{firstProductId}");

        // Assert - remove one item
        Assert.Equal(
            HttpStatusCode.NoContent,
            removeResponse.StatusCode);

        var remainingCart = await client.GetFromJsonAsync<CartResponse>(
            "/api/cart");

        Assert.NotNull(remainingCart);
        Assert.Equal(
            secondProductId,
            Assert.Single(remainingCart.Items).ProductId);

        // Act - clear cart
        using var clearResponse = await client.DeleteAsync("/api/cart");

        // Assert - clear cart
        Assert.Equal(
            HttpStatusCode.NoContent,
            clearResponse.StatusCode);

        var emptyCart = await client.GetFromJsonAsync<CartResponse>(
            "/api/cart");

        Assert.NotNull(emptyCart);
        Assert.Empty(emptyCart.Items);
        Assert.Equal(0, emptyCart.TotalQuantity);
        Assert.Equal(0m, emptyCart.TotalAmount);
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartIsEmpty_ReturnsConflictWithoutCreatingOrder()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var address = TestEntityFactory.CreateAddress(customerId);
            dbContext.CustomerAddresses.Add(address);

            await dbContext.SaveChangesAsync();
            addressId = address.Id;
        });

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/cart/checkout",
            new CheckoutCartRequest { AddressId = addressId });

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "The cart is empty.");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.False(await dbContext.Orders.AnyAsync());
            Assert.False(await dbContext.StockMovements.AnyAsync());
        });
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartIsValid_CreatesOrderAndConsumesCartAtomically()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        const string customerEmail = "buyer@example.com";
        using var client = factory.CreateCustomerClient(
            customerId,
            customerEmail);

        var productId = 0;
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var product = CreateProduct(
                "Mechanical Keyboard",
                price: 1250m,
                stockQuantity: 5);

            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                customerEmail));
            var address = TestEntityFactory.CreateAddress(
                customerId,
                recipientFullName: "Taylor Buyer");
            dbContext.CustomerAddresses.Add(address);
            dbContext.Carts.Add(new Cart
            {
                CustomerId = customerId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new()
                    {
                        Product = product,
                        Quantity = 2
                    }
                }
            });

            await dbContext.SaveChangesAsync();
            productId = product.Id;
            addressId = address.Id;
        });

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/cart/checkout",
            new CheckoutCartRequest { AddressId = addressId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        Assert.Equal(customerEmail, order.CustomerEmail);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(2500m, order.TotalAmount);
        Assert.Equal("TRY", order.Currency);
        Assert.NotNull(order.ShippingAddress);
        Assert.Equal(
            "Taylor Buyer",
            order.ShippingAddress.RecipientFullName);

        var orderItem = Assert.Single(order.Items);
        Assert.Equal(productId, orderItem.ProductId);
        Assert.Equal("Mechanical Keyboard", orderItem.ProductName);
        Assert.Equal(1250m, orderItem.UnitPrice);
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(2500m, orderItem.LineTotal);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/orders/{order.Id}",
            response.Headers.Location.AbsolutePath);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.False(await dbContext.Carts.AnyAsync());
            Assert.False(await dbContext.CartItems.AnyAsync());
            Assert.Single(await dbContext.Orders.ToArrayAsync());

            var stockQuantity = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.StockQuantity)
                .SingleAsync();

            Assert.Equal(3, stockQuantity);

            var movement = await dbContext.StockMovements
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(-2, movement.QuantityDelta);
            Assert.Equal(3, movement.StockQuantityAfter);
            Assert.Equal("Order placement", movement.Reason);
        });
    }

    [Fact]
    public async Task CheckoutAsync_WhenStockChangedAfterAddingItem_RollsBackAndKeepsCart()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var productId = 0;
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var product = CreateProduct(
                "Limited Product",
                stockQuantity: 1);

            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));
            var address = TestEntityFactory.CreateAddress(customerId);
            dbContext.CustomerAddresses.Add(address);
            dbContext.Carts.Add(new Cart
            {
                CustomerId = customerId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new()
                    {
                        Product = product,
                        Quantity = 2
                    }
                }
            });

            await dbContext.SaveChangesAsync();
            productId = product.Id;
            addressId = address.Id;
        });

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/cart/checkout",
            new CheckoutCartRequest { AddressId = addressId });

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            $"Product with ID {productId} does not have sufficient stock.");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.True(await dbContext.Carts.AnyAsync());
            Assert.True(await dbContext.CartItems.AnyAsync());
            Assert.False(await dbContext.Orders.AnyAsync());
            Assert.False(await dbContext.StockMovements.AnyAsync());

            var stockQuantity = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.StockQuantity)
                .SingleAsync();

            Assert.Equal(1, stockQuantity);
        });
    }

    [Fact]
    public async Task CheckoutAsync_WhenRepeated_CreatesOnlyOneOrder()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var productId = 0;
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var product = CreateProduct(
                "Keyboard",
                stockQuantity: 10);

            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));
            var address = TestEntityFactory.CreateAddress(customerId);
            dbContext.CustomerAddresses.Add(address);
            dbContext.Carts.Add(CreateCart(customerId, product));

            await dbContext.SaveChangesAsync();
            productId = product.Id;
            addressId = address.Id;
        });

        // Act
        using var firstResponse = await client.PostAsJsonAsync(
            "/api/cart/checkout",
            new CheckoutCartRequest { AddressId = addressId });
        using var secondResponse = await client.PostAsJsonAsync(
            "/api/cart/checkout",
            new CheckoutCartRequest { AddressId = addressId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        await ProblemDetailsAssertions.AssertProblemAsync(
            secondResponse,
            HttpStatusCode.Conflict,
            "Conflict",
            "The cart is empty.");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.Orders.CountAsync());
            Assert.Equal(1, await dbContext.StockMovements.CountAsync());

            var stockQuantity = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.StockQuantity)
                .SingleAsync();

            Assert.Equal(9, stockQuantity);
        });
    }

    private static Product CreateProduct(
        string name,
        decimal price = 1000m,
        int stockQuantity = 10)
    {
        return new Product
        {
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            IsActive = true,
            Category = new Category
            {
                Name = $"{name} Category",
                IsActive = true
            }
        };
    }

    private static Cart CreateCart(
        Guid customerId,
        Product product)
    {
        return new Cart
        {
            CustomerId = customerId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Items = new List<CartItem>
            {
                new()
                {
                    Product = product,
                    Quantity = 1
                }
            }
        };
    }
}
