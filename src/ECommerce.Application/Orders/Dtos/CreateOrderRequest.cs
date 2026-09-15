namespace ECommerce.Application.Orders.Dtos;

public sealed class CreateOrderRequest
{
    public int AddressId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
