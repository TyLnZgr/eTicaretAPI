namespace ECommerce.Api.Features.Orders.Dtos;

public sealed class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
