namespace ECommerce.Api.Features.Products.Dtos;

public sealed class ProductQueryParameters
{
    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
