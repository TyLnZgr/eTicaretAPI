using ECommerce.Api.Identity;

namespace ECommerce.Api.Models;

public class Order
{
    public int Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAtUtc { get; set; }
    public OrderAddressSnapshot? ShippingAddress { get; private set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
    public ApplicationUser? Customer { get; set; }

    public void SetShippingAddress(OrderAddressSnapshot shippingAddress)
    {
        ArgumentNullException.ThrowIfNull(shippingAddress);

        if (ShippingAddress is not null)
        {
            throw new InvalidOperationException(
                "The shipping address snapshot has already been assigned.");
        }

        ShippingAddress = shippingAddress;
    }

    public bool CanTransitionTo(OrderStatus nextStatus)
    {
        return (Status, nextStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Completed) => true,
            _ => false
        };
    }
}
