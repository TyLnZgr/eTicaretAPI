using ECommerce.Domain.Orders;

namespace ECommerce.Api.Tests.Domain.Orders;

public sealed class OrderTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 15, 13, 0, 0, DateTimeKind.Utc);
    private const string IdempotencyKey = "order-test-001";
    private const string RequestFingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Constructor_WhenValuesAreValid_NormalizesOrderIdentityData()
    {
        var shippingAddress = CreateSnapshot("Original Street No: 10");
        var customerId = Guid.NewGuid();

        var order = new Order(
            customerId,
            "  Customer@Example.COM  ",
            shippingAddress,
            CreatedAtUtc,
            IdempotencyKey,
            RequestFingerprint,
            "try");

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal("customer@example.com", order.CustomerEmail);
        Assert.Equal("TRY", order.Currency);
        Assert.Equal(CreatedAtUtc, order.CreatedAtUtc);
        Assert.Same(shippingAddress, order.ShippingAddress);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(IdempotencyKey, order.IdempotencyKey);
        Assert.Equal(RequestFingerprint, order.RequestFingerprint);
        Assert.Equal(0m, order.TotalAmount);
        Assert.Empty(order.Items);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("invalid key")]
    [InlineData("invalid/key")]
    public void Constructor_WhenIdempotencyKeyIsInvalid_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => new Order(
            Guid.NewGuid(),
            "customer@example.com",
            CreateSnapshot("Original Street No: 10"),
            CreatedAtUtc,
            key,
            RequestFingerprint));
    }

    [Theory]
    [InlineData("too-short")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void Constructor_WhenRequestFingerprintIsInvalid_Throws(
        string requestFingerprint)
    {
        Assert.Throws<ArgumentException>(() => new Order(
            Guid.NewGuid(),
            "customer@example.com",
            CreateSnapshot("Original Street No: 10"),
            CreatedAtUtc,
            IdempotencyKey,
            requestFingerprint));
    }

    [Fact]
    public void AddItem_WhenValuesAreValid_CalculatesLineAndOrderTotals()
    {
        var order = CreateOrder();

        var firstItem = order.AddItem(
            productId: 10,
            productName: "  Mechanical Keyboard  ",
            unitPrice: 1250.50m,
            quantity: 2);
        var secondItem = order.AddItem(
            productId: 20,
            productName: "Mouse",
            unitPrice: 499m,
            quantity: 1);

        Assert.Equal("Mechanical Keyboard", firstItem.ProductName);
        Assert.Equal(2501.00m, firstItem.LineTotal);
        Assert.Equal(499m, secondItem.LineTotal);
        Assert.Equal(3000.00m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void AddItem_WhenProductAlreadyExists_DoesNotChangeOrder()
    {
        var order = CreateOrder();

        order.AddItem(10, "Keyboard", 1000m, quantity: 1);

        Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(10, "Keyboard", 1000m, quantity: 2));

        Assert.Single(order.Items);
        Assert.Equal(1000m, order.TotalAmount);
    }

    [Theory]
    [InlineData(0, 1000, 1)]
    [InlineData(10, 0, 1)]
    [InlineData(10, 1000, 0)]
    [InlineData(10, 1000, OrderItem.MaxQuantity + 1)]
    public void AddItem_WhenNumericValuesAreInvalid_DoesNotChangeOrder(
        int productId,
        decimal unitPrice,
        int quantity)
    {
        var order = CreateOrder();

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() =>
            order.AddItem(productId, "Keyboard", unitPrice, quantity));

        Assert.Empty(order.Items);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_WhenLineTotalOverflows_DoesNotChangeOrder()
    {
        var order = CreateOrder();

        Assert.Throws<OverflowException>(() =>
            order.AddItem(10, "Keyboard", decimal.MaxValue, quantity: 2));

        Assert.Empty(order.Items);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void EnsureReadyForPlacement_WhenOrderHasNoItems_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(
            order.EnsureReadyForPlacement);
    }

    [Fact]
    public void EnsureReadyForPlacement_WhenOrderHasAnItem_DoesNotThrow()
    {
        var order = CreateOrder();
        order.AddItem(10, "Keyboard", 1000m, quantity: 1);

        var exception = Record.Exception(order.EnsureReadyForPlacement);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.PaymentProcessing)]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentProcessing, OrderStatus.Paid)]
    [InlineData(OrderStatus.PaymentProcessing, OrderStatus.Pending)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Completed)]
    public void IsTransitionAllowed_WhenTransitionIsDefined_ReturnsTrue(
        OrderStatus currentStatus,
        OrderStatus nextStatus)
    {
        Assert.True(Order.IsTransitionAllowed(currentStatus, nextStatus));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.PaymentProcessing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Completed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    public void IsTransitionAllowed_WhenTransitionIsNotDefined_ReturnsFalse(
        OrderStatus currentStatus,
        OrderStatus nextStatus)
    {
        Assert.False(Order.IsTransitionAllowed(currentStatus, nextStatus));
    }

    [Fact]
    public void TryTransitionTo_WhenTransitionIsAllowed_ChangesStatus()
    {
        var order = new Order();

        var wasChanged = order.TryTransitionTo(
            OrderStatus.PaymentProcessing);

        Assert.True(wasChanged);
        Assert.Equal(OrderStatus.PaymentProcessing, order.Status);
    }

    [Fact]
    public void TryTransitionTo_WhenTransitionIsInvalid_DoesNotChangeStatus()
    {
        var order = new Order();

        var wasChanged = order.TryTransitionTo(OrderStatus.Completed);

        Assert.False(wasChanged);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void TryTransitionTo_WhenPaymentFails_ReturnsOrderToPending()
    {
        var order = new Order();

        Assert.True(order.TryTransitionTo(OrderStatus.PaymentProcessing));
        Assert.True(order.TryTransitionTo(OrderStatus.Pending));

        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void TryTransitionTo_WhenOrderIsTerminal_RejectsEveryTransition(
        OrderStatus terminalStatus)
    {
        var order = new Order();

        if (terminalStatus == OrderStatus.Completed)
        {
            Assert.True(order.TryTransitionTo(OrderStatus.Paid));
            Assert.True(order.TryTransitionTo(OrderStatus.Shipped));
            Assert.True(order.TryTransitionTo(OrderStatus.Completed));
        }
        else
        {
            Assert.True(order.TryTransitionTo(OrderStatus.Cancelled));
        }

        foreach (var nextStatus in Enum.GetValues<OrderStatus>())
        {
            Assert.False(order.TryTransitionTo(nextStatus));
        }

        Assert.Equal(terminalStatus, order.Status);
    }

    [Fact]
    public void SetShippingAddress_WhenCalledTwice_RejectsReplacement()
    {
        // Arrange
        var order = new Order();
        var originalAddress = CreateSnapshot("Original Street No: 10");
        var replacementAddress = CreateSnapshot("Changed Street No: 99");

        order.SetShippingAddress(originalAddress);

        // Act
        var replaceAction = () =>
            order.SetShippingAddress(replacementAddress);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(
            replaceAction);

        Assert.Equal(
            "The shipping address snapshot has already been assigned.",
            exception.Message);
        Assert.Same(originalAddress, order.ShippingAddress);
    }

    private static OrderAddressSnapshot CreateSnapshot(
        string addressLine1)
    {
        return new OrderAddressSnapshot(
            "Taylor Customer",
            "+90 555 111 22 33",
            addressLine1,
            addressLine2: null,
            "Kadikoy",
            "Istanbul",
            "34710",
            "TR");
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            "customer@example.com",
            CreateSnapshot("Original Street No: 10"),
            CreatedAtUtc,
            IdempotencyKey,
            RequestFingerprint);
    }
}
