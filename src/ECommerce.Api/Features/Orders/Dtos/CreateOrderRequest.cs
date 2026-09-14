namespace ECommerce.Api.Features.Orders.Dtos;

public sealed class CreateOrderRequest
{
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
