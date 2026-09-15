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
            isActive: true);

        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(2499.90m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Same(category, product.Category);
        Assert.True(product.IsActive);
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
            isActive: true));
    }

    [Fact]
    public void Constructor_WhenStockIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product(
            "Keyboard",
            1000m,
            -1,
            CreateCategory(),
            isActive: true));
    }

    [Fact]
    public void UpdateDetails_WhenValid_ChangesOnlyProductDetails()
    {
        var product = CreateProduct(stockQuantity: 8);
        var newCategory = CreateCategory(id: 2);

        product.UpdateDetails(
            "  Updated Keyboard  ",
            1500m,
            newCategory,
            isActive: false);

        Assert.Equal("Updated Keyboard", product.Name);
        Assert.Equal(1500m, product.Price);
        Assert.Equal(8, product.StockQuantity);
        Assert.Equal(newCategory.Id, product.CategoryId);
        Assert.Same(newCategory, product.Category);
        Assert.False(product.IsActive);
    }

    [Fact]
    public void RecordInitialStock_WhenStockExists_AddsSingleMovement()
    {
        var product = CreateProduct(stockQuantity: 8);

        var movement = product.RecordInitialStock(OccurredAtUtc);

        Assert.NotNull(movement);
        Assert.Equal(8, movement.QuantityDelta);
        Assert.Equal(8, movement.StockQuantityAfter);
        Assert.Equal("Initial stock", movement.Reason);
        Assert.Single(product.StockMovements);

        Assert.Throws<InvalidOperationException>(() =>
            product.RecordInitialStock(OccurredAtUtc));
    }

    [Fact]
    public void AdjustStock_WhenDecreaseIsValid_UpdatesStockAndRecordsMovement()
    {
        var product = CreateProduct(stockQuantity: 8);

        var result = product.AdjustStock(
            quantityDelta: -3,
            reason: "  Customer order  ",
            OccurredAtUtc);

        Assert.Equal(ProductStockChangeStatus.Success, result.Status);
        Assert.Equal(5, product.StockQuantity);
        Assert.NotNull(result.Movement);
        Assert.Equal(-3, result.Movement.QuantityDelta);
        Assert.Equal(5, result.Movement.StockQuantityAfter);
        Assert.Equal("Customer order", result.Movement.Reason);
        Assert.Single(product.StockMovements);
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

        var result = product.AdjustStock(
            quantityDelta,
            reason,
            OccurredAtUtc);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Movement);
        Assert.Equal(8, product.StockQuantity);
        Assert.Empty(product.StockMovements);
    }

    private static Product CreateProduct(int stockQuantity)
    {
        return new Product(
            "Keyboard",
            1000m,
            stockQuantity,
            CreateCategory(),
            isActive: true);
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
