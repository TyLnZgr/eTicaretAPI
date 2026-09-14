using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Orders.Endpoints;

public sealed class OrderEndpointTests
{
    [Fact]
    public async Task CreateAsync_AsCustomer_CreatesOwnedOrderAndDecreasesStock()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        const string customerEmail = "buyer@example.com";
        using var client = factory.CreateCustomerClient(
            customerId,
            customerEmail);

        var productId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                customerEmail));

            var product = new Product
            {
                Name = "Mechanical Keyboard",
                Price = 1250m,
                StockQuantity = 5,
                IsActive = true,
                Category = new Category
                {
                    Name = "Computer Accessories",
                    IsActive = true
                }
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        });

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        };

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        Assert.Equal(customerEmail, order.CustomerEmail);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(2500m, order.TotalAmount);
        Assert.Single(order.Items);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/orders/{order.Id}",
            response.Headers.Location.AbsolutePath);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var savedOrder = await dbContext.Orders
                .AsNoTracking()
                .Include(candidate => candidate.Items)
                .SingleAsync();

            Assert.Equal(customerId, savedOrder.CustomerId);
            Assert.Equal(customerEmail, savedOrder.CustomerEmail);
            Assert.Equal(2500m, savedOrder.TotalAmount);
            Assert.Equal(2, Assert.Single(savedOrder.Items).Quantity);

            var product = await dbContext.Products
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == productId);

            Assert.Equal(3, product.StockQuantity);

            var movement = await dbContext.StockMovements
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(-2, movement.QuantityDelta);
            Assert.Equal(3, movement.StockQuantityAfter);
            Assert.Equal("Order placement", movement.Reason);
        });
    }

    [Fact]
    public async Task GetByIdAsync_AsDifferentCustomer_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var orderId = 0;

        using var ownerClient = factory.CreateCustomerClient(
            ownerId,
            "owner@example.com");
        using var intruderClient = factory.CreateCustomerClient(
            intruderId,
            "intruder@example.com");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                ownerId,
                "owner@example.com"));

            var order = CreateOrder(
                ownerId,
                "owner@example.com");

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            orderId = order.Id;
        });

        // Act
        using var intruderResponse = await intruderClient.GetAsync(
            $"/api/orders/{orderId}");
        using var ownerResponse = await ownerClient.GetAsync(
            $"/api/orders/{orderId}");

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            intruderResponse,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Order with ID {orderId} was not found.");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_AsCustomerWhenTargetIsPaid_ReturnsForbidden()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        var orderId = 0;
        using var client = factory.CreateCustomerClient(
            customerId,
            "customer@example.com");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var order = CreateOrder(
                customerId,
                "customer@example.com");

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            orderId = order.Id;
        });

        // Act
        using var response = await client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new UpdateOrderStatusRequest
            {
                Status = "Paid"
            });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var status = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.Id == orderId)
                .Select(order => order.Status)
                .SingleAsync();

            Assert.Equal(OrderStatus.Pending, status);
        });
    }

    [Fact]
    public async Task CancelAsync_AsOwner_CancelsOrderAndRestoresStock()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        var orderId = 0;
        var productId = 0;
        using var client = factory.CreateCustomerClient(
            customerId,
            "customer@example.com");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var product = new Product
            {
                Name = "Mouse",
                Price = 500m,
                StockQuantity = 3,
                IsActive = true,
                Category = new Category
                {
                    Name = "Accessories",
                    IsActive = true
                }
            };

            var order = CreateOrder(
                customerId,
                "customer@example.com");

            order.Items.Add(new OrderItem
            {
                Product = product,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 2,
                LineTotal = 1000m
            });

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            orderId = order.Id;
            productId = product.Id;
        });

        // Act
        using var response = await client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new UpdateOrderStatusRequest
            {
                Status = "Cancelled"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedOrder = await response.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(updatedOrder);
        Assert.Equal("Cancelled", updatedOrder.Status);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var productStock = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId)
                .Select(product => product.StockQuantity)
                .SingleAsync();

            Assert.Equal(5, productStock);

            var movement = await dbContext.StockMovements
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(2, movement.QuantityDelta);
            Assert.Equal(5, movement.StockQuantityAfter);
            Assert.Equal(
                $"Order {orderId} cancellation",
                movement.Reason);
        });
    }

    [Fact]
    public async Task AdminList_AsCustomer_ReturnsForbidden()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.GetAsync(
            "/api/admin/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminList_AsAdministrator_FiltersByNormalizedEmail()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.AddRange(
                TestEntityFactory.CreateUser(
                    firstCustomerId,
                    "first@example.com"),
                TestEntityFactory.CreateUser(
                    secondCustomerId,
                    "second@example.com"));

            dbContext.Orders.AddRange(
                CreateOrder(firstCustomerId, "first@example.com"),
                CreateOrder(secondCustomerId, "second@example.com"));

            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.GetAsync(
            "/api/admin/orders" +
            "?customerEmail=FIRST%40EXAMPLE.COM" +
            "&page=1&pageSize=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<AdminOrderResponse>>();

        Assert.NotNull(result);
        var order = Assert.Single(result.Items);

        Assert.Equal(firstCustomerId, order.CustomerId);
        Assert.Equal("first@example.com", order.CustomerEmail);
        Assert.Equal(1, result.TotalCount);

        var responseJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "passwordHash",
            responseJson,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "securityStamp",
            responseJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminUpdateStatus_AsAdministrator_ChangesPendingOrderToPaid()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var customerId = Guid.NewGuid();
        var orderId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var order = CreateOrder(
                customerId,
                "customer@example.com");

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            orderId = order.Id;
        });

        // Act
        using var response = await client.PatchAsJsonAsync(
            $"/api/admin/orders/{orderId}/status",
            new UpdateOrderStatusRequest
            {
                Status = "Paid"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        Assert.Equal("Paid", order.Status);
    }

    private static Order CreateOrder(
        Guid customerId,
        string customerEmail)
    {
        return new Order
        {
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            TotalAmount = 100m,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
