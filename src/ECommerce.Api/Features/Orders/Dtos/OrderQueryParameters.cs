namespace ECommerce.Api.Features.Orders.Dtos;

public sealed class OrderQueryParameters
{
    public string? Status { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
