namespace ECommerce.Application.Orders.Dtos;

public sealed class AdminOrderQueryParameters
{
    public Guid? CustomerId { get; init; }
    public string? CustomerEmail { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
