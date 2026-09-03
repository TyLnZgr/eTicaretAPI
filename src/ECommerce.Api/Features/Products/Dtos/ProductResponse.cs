namespace ECommerce.Api.Features.Products.Dtos;

public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    int CategoryId,
    string CategoryName);
