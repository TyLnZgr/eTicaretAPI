using ECommerce.Api.Identity;

namespace ECommerce.Api.Models;

public class Order
{
    public int Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
    public ApplicationUser? Customer { get; set; }

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
