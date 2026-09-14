namespace ECommerce.Api.Features.Orders.Dtos;

public sealed class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
