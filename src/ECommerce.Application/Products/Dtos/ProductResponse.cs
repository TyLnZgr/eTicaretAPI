namespace ECommerce.Application.Products.Dtos;

public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    long Version);
