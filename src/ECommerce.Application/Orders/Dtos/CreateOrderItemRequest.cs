namespace ECommerce.Application.Orders.Dtos;

public sealed class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
