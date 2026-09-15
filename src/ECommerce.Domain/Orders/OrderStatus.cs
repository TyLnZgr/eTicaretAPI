namespace ECommerce.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5,
    PaymentProcessing = 6
}