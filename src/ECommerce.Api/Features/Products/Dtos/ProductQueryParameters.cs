namespace ECommerce.Api.Features.Products.Dtos;

public sealed class ProductQueryParameters
{
    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public bool? IsActive { get; init; }
    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }
}