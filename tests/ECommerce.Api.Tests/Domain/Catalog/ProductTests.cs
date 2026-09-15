using ECommerce.Domain.Catalog;

namespace ECommerce.Api.Tests.Domain.Catalog;

public sealed class ProductTests
{
    private static readonly DateTime OccurredAtUtc =
        new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenDetailsAreValid_CreatesNormalizedProduct()
    {
        var category = CreateCategory(id: 12);

        var product = new Product(
            "  Mechanical Keyboard  ",
            2499.90m,
            10,
            category,
            isActive: true,
            OccurredAtUtc);

        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(2499.90m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Same(category, product.Category);
        Assert.True(product.IsActive);
        Assert.Equal(1, product.Version);
        Assert.Equal(OccurredAtUtc, product.CreatedAtUtc);
        Assert.Equal(OccurredAtUtc, product.UpdatedAtUtc);
        Assert.Null(product.DeletedAtUtc);
        Assert.False(product.IsDeleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPriceIsNotPositive_Throws(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product(
            "Keyboard",
            price,
            10,
            CreateCategory(),
            isActive: true,
            OccurredAtUtc));
    }

    [Fact]
    public void Constructor_WhenStockIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product(
            "Keyboard",
            1000m,
            -1,
            CreateCategory(),
            isActive: true,
            OccurredAtUtc));
    }

    [Fact]
    public void UpdateDetails_WhenValid_ChangesOnlyProductDetails()
    {
        var product = CreateProduct(stockQuantity: 8);
        var newCategory = CreateCategory(id: 2);
        var originalVersion = product.Version;

        product.UpdateDetails(
            "  Updated Keyboard  ",
            1500m,
            newCategory,
            isActive: false,
            OccurredAtUtc.AddMinutes(5));

        Assert.Equal("Updated Keyboard", product.Name);
        Assert.Equal(1500m, product.Price);
        Assert.Equal(8, product.StockQuantity);
        Assert.Equal(newCategory.Id, product.CategoryId);
        Assert.Same(newCategory, product.Category);
        Assert.False(product.IsActive);
        Assert.Equal(originalVersion + 1, product.Version);
        Assert.Equal(
            OccurredAtUtc.AddMinutes(5),
            product.UpdatedAtUtc);
    }

    [Fact]
    public void RecordInitialStock_WhenStockExists_AddsSingleMovement()
    {
        var product = CreateProduct(stockQuantity: 8);
        var originalVersion = product.Version;

        var movement = product.RecordInitialStock(OccurredAtUtc);

        Assert.NotNull(movement);
        Assert.Equal(8, movement.QuantityDelta);
        Assert.Equal(8, movement.StockQuantityAfter);
        Assert.Equal("Initial stock", movement.Reason);
        Assert.Single(product.StockMovements);

        Assert.Throws<InvalidOperationException>(() =>
            product.RecordInitialStock(OccurredAtUtc));
        Assert.Equal(originalVersion, product.Version);
    }

    [Fact]
    public void AdjustStock_WhenDecreaseIsValid_UpdatesStockAndRecordsMovement()
    {
        var product = CreateProduct(stockQuantity: 8);
        var originalVersion = product.Version;
        var adjustedAtUtc = OccurredAtUtc.AddMinutes(5);

        var result = product.AdjustStock(
            quantityDelta: -3,
            reason: "  Customer order  ",
            adjustedAtUtc);

        Assert.Equal(ProductStockChangeStatus.Success, result.Status);
        Assert.Equal(5, product.StockQuantity);
        Assert.NotNull(result.Movement);
        Assert.Equal(-3, result.Movement.QuantityDelta);
        Assert.Equal(5, result.Movement.StockQuantityAfter);
        Assert.Equal("Customer order", result.Movement.Reason);
        Assert.Single(product.StockMovements);
        Assert.Equal(originalVersion + 1, product.Version);
        Assert.Equal(adjustedAtUtc, product.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(-9, "Customer order", ProductStockChangeStatus.InsufficientStock)]
    [InlineData(0, "Stock count", ProductStockChangeStatus.InvalidQuantityDelta)]
    [InlineData(1, "   ", ProductStockChangeStatus.InvalidReason)]
    public void AdjustStock_WhenRequestIsInvalid_DoesNotChangeState(
        int quantityDelta,
        string reason,
        ProductStockChangeStatus expectedStatus)
    {
        var product = CreateProduct(stockQuantity: 8);
        var originalVersion = product.Version;

        var result = product.AdjustStock(
            quantityDelta,
            reason,
            OccurredAtUtc);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Movement);
        Assert.Equal(8, product.StockQuantity);
        Assert.Empty(product.StockMovements);
        Assert.Equal(originalVersion, product.Version);
    }

    [Fact]
    public void MarkAsDeleted_WhenProductExists_ChangesLifecycleState()
    {
        var product = CreateProduct(stockQuantity: 8);
        var deletedAtUtc = OccurredAtUtc.AddMinutes(10);
        var originalVersion = product.Version;

        var wasChanged = product.MarkAsDeleted(deletedAtUtc);

        Assert.True(wasChanged);
        Assert.True(product.IsDeleted);
        Assert.False(product.IsActive);
        Assert.Equal(deletedAtUtc, product.DeletedAtUtc);
        Assert.Equal(deletedAtUtc, product.UpdatedAtUtc);
        Assert.Equal(originalVersion + 1, product.Version);
    }

    [Fact]
    public void MarkAsDeleted_WhenRepeated_DoesNotChangeStateAgain()
    {
        var product = CreateProduct(stockQuantity: 8);
        var firstDeletedAtUtc = OccurredAtUtc.AddMinutes(10);

        Assert.True(product.MarkAsDeleted(firstDeletedAtUtc));
        var deletedVersion = product.Version;

        var wasChanged = product.MarkAsDeleted(
            OccurredAtUtc.AddMinutes(20));

        Assert.False(wasChanged);
        Assert.Equal(firstDeletedAtUtc, product.DeletedAtUtc);
        Assert.Equal(firstDeletedAtUtc, product.UpdatedAtUtc);
        Assert.Equal(deletedVersion, product.Version);
    }

    [Fact]
    public void UpdateDetails_WhenProductIsDeleted_Throws()
    {
        var product = CreateProduct(stockQuantity: 8);
        product.MarkAsDeleted(OccurredAtUtc.AddMinutes(10));

        Assert.Throws<InvalidOperationException>(() =>
            product.UpdateDetails(
                "Updated Keyboard",
                1500m,
                CreateCategory(id: 2),
                isActive: true,
                OccurredAtUtc.AddMinutes(20)));
    }

    [Fact]
    public void UpdateDetails_WhenTimeMovesBackward_DoesNotChangeState()
    {
        var product = CreateProduct(stockQuantity: 8);

        product.UpdateDetails(
            "First Update",
            1200m,
            CreateCategory(id: 2),
            isActive: true,
            OccurredAtUtc.AddMinutes(10));
        var versionAfterFirstUpdate = product.Version;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateDetails(
                "Older Update",
                1300m,
                CreateCategory(id: 3),
                isActive: false,
                OccurredAtUtc.AddMinutes(5)));

        Assert.Equal("First Update", product.Name);
        Assert.Equal(1200m, product.Price);
        Assert.True(product.IsActive);
        Assert.Equal(versionAfterFirstUpdate, product.Version);
        Assert.Equal(
            OccurredAtUtc.AddMinutes(10),
            product.UpdatedAtUtc);
    }

    private static Product CreateProduct(int stockQuantity)
    {
        return new Product(
            "Keyboard",
            1000m,
            stockQuantity,
            CreateCategory(),
            isActive: true,
            OccurredAtUtc);
    }

    private static Category CreateCategory(int id = 1)
    {
        return new Category
        {
            Id = id,
            Name = "Accessories",
            IsActive = true
        };
    }
}
