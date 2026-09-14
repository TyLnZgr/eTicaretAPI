namespace ECommerce.Api.Features.Operations.Outbox.Dtos;

public sealed class OutboxMessageQueryParameters
{
    public string? Status { get; init; }
    public string? Type { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
