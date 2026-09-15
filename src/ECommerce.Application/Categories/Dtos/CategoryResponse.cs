namespace ECommerce.Application.Categories.Dtos;

public sealed record CategoryResponse(
    int Id,
    string Name,
    bool IsActive);
