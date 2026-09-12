using ECommerce.Api.Data;
using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Products.Services;

public sealed class EfCoreProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenSortedByPriceAndPaged_ReturnsExpectedPage()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var dbContext = database.DbContext;

        var category = new Category
        {
            Name = "Test Category",
            IsActive = true
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        dbContext.Products.AddRange(
            new Product
            {
                Name = "Expensive Product",
                Price = 2799.90m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = category.Id,
                Category = category
            },
            new Product
            {
                Name = "Affordable Product",
                Price = 999.90m,
                StockQuantity = 20,
                IsActive = true,
                CategoryId = category.Id,
                Category = category
            },
            new Product
            {
                Name = "Medium Product",
                Price = 1499.90m,
                StockQuantity = 15,
                IsActive = true,
                CategoryId = category.Id,
                Category = category
            });

        await dbContext.SaveChangesAsync();

        var service = new EfCoreProductService(
            dbContext,
            TimeProvider.System);

        var queryParameters = new ProductQueryParameters
        {
            SortBy = "price",
            SortDirection = "asc",
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = await service.GetAllAsync(queryParameters);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);

        Assert.Collection(
            result.Items,
            first => Assert.Equal(999.90m, first.Price),
            second => Assert.Equal(1499.90m, second.Price));
    }

    [Fact]
    public async Task GetAllAsync_WhenFiltersAreCombined_ReturnsOnlyMatchingProduct()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var dbContext = database.DbContext;

        var electronics = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        var office = new Category
        {
            Name = "Office",
            IsActive = true
        };

        dbContext.Categories.AddRange(
            electronics,
            office);

        await dbContext.SaveChangesAsync();

        dbContext.Products.AddRange(
            new Product
            {
                Name = "USB-C Hub",
                Price = 1500m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = electronics.Id,
                Category = electronics
            },
            new Product
            {
                Name = "USB-C Cable",
                Price = 1200m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = office.Id,
                Category = office
            },
            new Product
            {
                Name = "USB-C Mouse",
                Price = 1200m,
                StockQuantity = 10,
                IsActive = false,
                CategoryId = electronics.Id,
                Category = electronics
            },
            new Product
            {
                Name = "USB-C Adapter",
                Price = 500m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = electronics.Id,
                Category = electronics
            },
            new Product
            {
                Name = "USB-C Dock",
                Price = 2500m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = electronics.Id,
                Category = electronics
            },
            new Product
            {
                Name = "Mechanical Keyboard",
                Price = 1500m,
                StockQuantity = 10,
                IsActive = true,
                CategoryId = electronics.Id,
                Category = electronics
            });

        await dbContext.SaveChangesAsync();

        var service = new EfCoreProductService(
            dbContext,
            TimeProvider.System);

        var queryParameters = new ProductQueryParameters
        {
            Search = "USB-C",
            CategoryId = electronics.Id,
            IsActive = true,
            MinPrice = 1000m,
            MaxPrice = 2000m,
            SortBy = "name",
            SortDirection = "asc",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(
            queryParameters);

        // Assert
        var product = Assert.Single(result.Items);

        Assert.Equal("USB-C Hub", product.Name);
        Assert.Equal(electronics.Id, product.CategoryId);
        Assert.True(product.IsActive);
        Assert.InRange(product.Price, 1000m, 2000m);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task CreateAsync_WhenInitialStockIsPositive_CreatesOpeningMovement()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var category = new Category
        {
            Name = "Test Category",
            IsActive = true
        };

        database.DbContext.Categories.Add(category);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreProductService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var result = await service.CreateAsync(
            name: "New Product",
            price: 100m,
            stockQuantity: 8,
            categoryId: category.Id,
            isActive: true);

        // Assert
        Assert.Equal(ProductMutationStatus.Success, result.Status);
        Assert.NotNull(result.Product);

        var movement = await database.DbContext.StockMovements
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(result.Product.Id, movement.ProductId);
        Assert.Equal(8, movement.QuantityDelta);
        Assert.Equal(8, movement.StockQuantityAfter);
        Assert.Equal("Initial stock", movement.Reason);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenDecreaseIsValid_UpdatesStock()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var product = new Product
        {
            Name = "Stocked Product",
            Price = 100m,
            StockQuantity = 5,
            IsActive = true,
            Category = new Category
            {
                Name = "Test Category",
                IsActive = true
            }
        };

        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service =
            new EfCoreProductService(
                database.DbContext,
                TimeProvider.System);

        // Act
        var status = await service.AdjustStockAsync(
            product.Id,
            quantityDelta: -3,
            reason: "Customer order");

        // Assert
        Assert.Equal(
            ProductStockAdjustmentStatus.Success,
            status);

        await database.DbContext.Entry(product).ReloadAsync();

        Assert.Equal(2, product.StockQuantity);

        var movement = await database.DbContext.StockMovements
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(product.Id, movement.ProductId);
        Assert.Equal(-3, movement.QuantityDelta);
        Assert.Equal(2, movement.StockQuantityAfter);
        Assert.Equal("Customer order", movement.Reason);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenDecreaseExceedsStock_DoesNotChangeStock()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var product = new Product
        {
            Name = "Low Stock Product",
            Price = 100m,
            StockQuantity = 2,
            IsActive = true,
            Category = new Category
            {
                Name = "Test Category",
                IsActive = true
            }
        };

        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service =
            new EfCoreProductService(
                database.DbContext,
                TimeProvider.System);

        // Act
        var status = await service.AdjustStockAsync(
            product.Id,
            quantityDelta: -3,
            reason: "Customer order");

        // Assert
        Assert.Equal(
            ProductStockAdjustmentStatus.InsufficientStock,
            status);

        await database.DbContext.Entry(product).ReloadAsync();

        Assert.Equal(2, product.StockQuantity);
        Assert.False(
            await database.DbContext.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task AdjustStockAsync_WhenIncreaseExceedsIntMax_DoesNotChangeStock()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var product = new Product
        {
            Name = "Maximum Stock Product",
            Price = 100m,
            StockQuantity = int.MaxValue,
            IsActive = true,
            Category = new Category
            {
                Name = "Test Category",
                IsActive = true
            }
        };

        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service =
            new EfCoreProductService(
                database.DbContext,
                TimeProvider.System);

        // Act
        var status = await service.AdjustStockAsync(
            product.Id,
            quantityDelta: 1,
            reason: "Warehouse delivery");

        // Assert
        Assert.Equal(
            ProductStockAdjustmentStatus.StockLimitExceeded,
            status);

        await database.DbContext.Entry(product).ReloadAsync();

        Assert.Equal(int.MaxValue, product.StockQuantity);
        Assert.False(
            await database.DbContext.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task AdjustStockAsync_WhenReasonIsInvalid_DoesNotChangeStock()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var product = new Product
        {
            Name = "Stocked Product",
            Price = 100m,
            StockQuantity = 5,
            IsActive = true,
            Category = new Category
            {
                Name = "Test Category",
                IsActive = true
            }
        };

        database.DbContext.Products.Add(product);
        await database.DbContext.SaveChangesAsync();

        var service = new EfCoreProductService(
            database.DbContext,
            TimeProvider.System);

        // Act
        var status = await service.AdjustStockAsync(
            product.Id,
            quantityDelta: -1,
            reason: "   ");

        // Assert
        Assert.Equal(
            ProductStockAdjustmentStatus.InvalidReason,
            status);

        await database.DbContext.Entry(product).ReloadAsync();

        Assert.Equal(5, product.StockQuantity);
        Assert.False(
            await database.DbContext.StockMovements.AnyAsync());
    }
}
