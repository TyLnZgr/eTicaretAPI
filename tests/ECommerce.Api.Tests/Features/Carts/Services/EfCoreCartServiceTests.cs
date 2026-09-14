using ECommerce.Api.Features.Carts.Outcomes;
using ECommerce.Api.Features.Carts.Services;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Carts.Services;

public sealed class EfCoreCartServiceTests
{
    [Fact]
    public async Task SetItemQuantityAsync_WhenRepeated_ReplacesQuantityWithoutReservingStock()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        var product = CreateProduct(stockQuantity: 10);

        database.DbContext.Users.Add(TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com"));
        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreCartService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var firstResult = await service.SetItemQuantityAsync(
            customerId,
            product.Id,
            quantity: 2);

        var secondResult = await service.SetItemQuantityAsync(
            customerId,
            product.Id,
            quantity: 4);

        // Assert
        Assert.Equal(CartMutationStatus.Success, firstResult.Status);
        Assert.Equal(CartMutationStatus.Success, secondResult.Status);

        database.DbContext.ChangeTracker.Clear();

        var cart = await database.DbContext.Carts
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleAsync();

        var item = Assert.Single(cart.Items);

        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(4, item.Quantity);

        var stockQuantity = await database.DbContext.Products
            .AsNoTracking()
            .Where(candidate => candidate.Id == product.Id)
            .Select(candidate => candidate.StockQuantity)
            .SingleAsync();

        Assert.Equal(10, stockQuantity);
    }

    [Fact]
    public async Task SetItemQuantityAsync_WhenStockIsInsufficient_DoesNotCreateCart()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        var product = CreateProduct(stockQuantity: 1);

        database.DbContext.Users.Add(TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com"));
        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreCartService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var result = await service.SetItemQuantityAsync(
            customerId,
            product.Id,
            quantity: 2);

        // Assert
        Assert.Equal(
            CartMutationStatus.InsufficientStock,
            result.Status);
        Assert.Equal(1, result.AvailableStock);
        Assert.False(await database.DbContext.Carts.AnyAsync());
        Assert.False(await database.DbContext.CartItems.AnyAsync());
    }

    [Fact]
    public async Task RemoveAndClearAsync_KeepCartButRemoveRequestedItems()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        var firstProduct = CreateProduct(
            "Keyboard",
            stockQuantity: 10);
        var secondProduct = CreateProduct(
            "Mouse",
            stockQuantity: 10);

        var cart = new Cart
        {
            CustomerId = customerId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Items = new List<CartItem>
            {
                new()
                {
                    Product = firstProduct,
                    Quantity = 1
                },
                new()
                {
                    Product = secondProduct,
                    Quantity = 2
                }
            }
        };

        database.DbContext.Users.Add(TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com"));
        database.DbContext.Carts.Add(cart);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreCartService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var removalStatus = await service.RemoveItemAsync(
            customerId,
            firstProduct.Id);

        await service.ClearAsync(customerId);

        // Assert
        Assert.Equal(
            CartItemRemovalStatus.Success,
            removalStatus);

        database.DbContext.ChangeTracker.Clear();

        Assert.True(await database.DbContext.Carts.AnyAsync());
        Assert.False(await database.DbContext.CartItems.AnyAsync());
    }

    private static Product CreateProduct(
        string name = "Keyboard",
        int stockQuantity = 10)
    {
        return new Product
        {
            Name = name,
            Price = 1000m,
            StockQuantity = stockQuantity,
            IsActive = true,
            Category = new Category
            {
                Name = $"{name} Category",
                IsActive = true
            }
        };
    }
}
