using ECommerce.Domain.Carts;

namespace ECommerce.Api.Tests.Domain.Carts;

public sealed class CartTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc =
        CreatedAtUtc.AddMinutes(5);

    [Fact]
    public void Constructor_WhenValuesAreValid_CreatesEmptyCart()
    {
        var customerId = Guid.NewGuid();

        var cart = new Cart(customerId, CreatedAtUtc);

        Assert.Equal(customerId, cart.CustomerId);
        Assert.Equal(CreatedAtUtc, cart.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, cart.UpdatedAtUtc);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Constructor_WhenCustomerIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cart(Guid.Empty, CreatedAtUtc));
    }

    [Fact]
    public void SetItemQuantity_WhenProductIsNew_AddsItemAndTouchesCart()
    {
        var cart = CreateCart();

        var wasChanged = cart.SetItemQuantity(
            productId: 10,
            quantity: 2,
            UpdatedAtUtc);

        Assert.True(wasChanged);
        var item = Assert.Single(cart.Items);
        Assert.Equal(10, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(UpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void SetItemQuantity_WhenProductExists_ReplacesQuantityWithoutDuplicate()
    {
        var cart = CreateCart();
        cart.SetItemQuantity(10, quantity: 2, CreatedAtUtc);

        var wasChanged = cart.SetItemQuantity(
            10,
            quantity: 4,
            UpdatedAtUtc);

        Assert.True(wasChanged);
        var item = Assert.Single(cart.Items);
        Assert.Equal(4, item.Quantity);
        Assert.Equal(UpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void SetItemQuantity_WhenQuantityIsUnchanged_DoesNotTouchCart()
    {
        var cart = CreateCart();
        cart.SetItemQuantity(10, quantity: 2, CreatedAtUtc);

        var wasChanged = cart.SetItemQuantity(
            10,
            quantity: 2,
            UpdatedAtUtc);

        Assert.False(wasChanged);
        Assert.Equal(CreatedAtUtc, cart.UpdatedAtUtc);
        Assert.Single(cart.Items);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(10, 0)]
    [InlineData(10, CartItem.MaximumQuantity + 1)]
    public void SetItemQuantity_WhenValuesAreInvalid_DoesNotChangeCart(
        int productId,
        int quantity)
    {
        var cart = CreateCart();

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() =>
            cart.SetItemQuantity(productId, quantity, UpdatedAtUtc));

        Assert.Empty(cart.Items);
        Assert.Equal(CreatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WhenProductExists_RemovesItemAndTouchesCart()
    {
        var cart = CreateCart();
        cart.SetItemQuantity(10, quantity: 2, CreatedAtUtc);

        var wasRemoved = cart.RemoveItem(10, UpdatedAtUtc);

        Assert.True(wasRemoved);
        Assert.Empty(cart.Items);
        Assert.Equal(UpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WhenProductDoesNotExist_DoesNotTouchCart()
    {
        var cart = CreateCart();

        var wasRemoved = cart.RemoveItem(10, UpdatedAtUtc);

        Assert.False(wasRemoved);
        Assert.Equal(CreatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void Clear_WhenCartHasItems_RemovesAllItemsAndTouchesCart()
    {
        var cart = CreateCart();
        cart.SetItemQuantity(10, quantity: 1, CreatedAtUtc);
        cart.SetItemQuantity(20, quantity: 2, CreatedAtUtc);

        var wasChanged = cart.Clear(UpdatedAtUtc);

        Assert.True(wasChanged);
        Assert.Empty(cart.Items);
        Assert.Equal(UpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void Clear_WhenCartIsEmpty_DoesNotTouchCart()
    {
        var cart = CreateCart();

        var wasChanged = cart.Clear(UpdatedAtUtc);

        Assert.False(wasChanged);
        Assert.Equal(CreatedAtUtc, cart.UpdatedAtUtc);
    }

    private static Cart CreateCart()
    {
        return new Cart(Guid.NewGuid(), CreatedAtUtc);
    }
}
