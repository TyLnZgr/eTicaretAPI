using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using ECommerce.Infrastructure.Persistence.Services.Orders;
using ECommerce.Domain.Catalog;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ECommerce.Domain.Orders;

namespace ECommerce.Api.Tests.Features.Orders.Services;

public sealed class EfCoreOrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenOneProductHasInsufficientStock_DoesNotMutateDatabase()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        var customer = TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com");
        var address = TestEntityFactory.CreateAddress(customerId);
        var category = new Category
        {
            Name = "Accessories",
            IsActive = true
        };

        var availableProduct = new Product
        {
            Name = "Keyboard",
            Price = 1000m,
            StockQuantity = 5,
            IsActive = true,
            Category = category
        };

        var lowStockProduct = new Product
        {
            Name = "Mouse",
            Price = 500m,
            StockQuantity = 1,
            IsActive = true,
            Category = category
        };

        database.DbContext.Users.Add(customer);
        database.DbContext.CustomerAddresses.Add(address);
        database.DbContext.Products.AddRange(
            availableProduct,
            lowStockProduct);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreOrderPlacementService(
            database.DbContext,
            TimeProvider.System);

        var items = new[]
        {
            new CreateOrderItemRequest
            {
                ProductId = availableProduct.Id,
                Quantity = 2
            },
            new CreateOrderItemRequest
            {
                ProductId = lowStockProduct.Id,
                Quantity = 2
            }
        };

        // Act
        var result = await service.CreateAsync(
            customerId,
            address.Id,
            items);

        // Assert
        Assert.Equal(
            OrderCreationStatus.InsufficientStock,
            result.Status);
        Assert.Equal(lowStockProduct.Id, result.ProductId);

        database.DbContext.ChangeTracker.Clear();

        var products = await database.DbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToArrayAsync();

        Assert.Equal(5, products[0].StockQuantity);
        Assert.Equal(1, products[1].StockQuantity);
        Assert.False(await database.DbContext.Orders.AnyAsync());
        Assert.False(await database.DbContext.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenCancellationIsRepeated_RestoresStockOnlyOnce()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Keyboard",
            Price = 1000m,
            StockQuantity = 3,
            IsActive = true,
            Category = new Category
            {
                Name = "Accessories",
                IsActive = true
            }
        };

        var order = new Order
        {
            CustomerId = customerId,
            CustomerEmail = "customer@example.com",
            TotalAmount = 2000m,
            CreatedAtUtc = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new()
                {
                    Product = product,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = 2,
                    LineTotal = 2000m
                }
            }
        };

        database.DbContext.Users.Add(TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com"));
        database.DbContext.Orders.Add(order);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreOrderService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var firstResult = await service.UpdateStatusAsync(
            order.Id,
            customerId,
            OrderStatus.Cancelled);

        var secondResult = await service.UpdateStatusAsync(
            order.Id,
            customerId,
            OrderStatus.Cancelled);

        // Assert
        Assert.Equal(
            OrderStatusUpdateStatus.Success,
            firstResult.Status);
        Assert.Equal(
            OrderStatusUpdateStatus.InvalidTransition,
            secondResult.Status);
        Assert.Equal(
            OrderStatus.Cancelled,
            secondResult.CurrentStatus);

        database.DbContext.ChangeTracker.Clear();

        var stockQuantity = await database.DbContext.Products
            .AsNoTracking()
            .Where(candidate => candidate.Id == product.Id)
            .Select(candidate => candidate.StockQuantity)
            .SingleAsync();

        Assert.Equal(5, stockQuantity);

        var movement = await database.DbContext.StockMovements
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(2, movement.QuantityDelta);
        Assert.Equal(5, movement.StockQuantityAfter);
    }

}
