using ECommerce.Domain.Orders;

namespace ECommerce.Api.Tests.Features.Orders.Models;

public sealed class OrderTests
{
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
}
