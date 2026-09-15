namespace ECommerce.Application.Carts.Dtos;

public sealed record CartResponse(
    int TotalQuantity,
    decimal TotalAmount,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<CartItemResponse> Items)
{
    public static CartResponse Empty { get; } = new(
        TotalQuantity: 0,
        TotalAmount: 0m,
        UpdatedAtUtc: null,
        Items: Array.Empty<CartItemResponse>());
}
