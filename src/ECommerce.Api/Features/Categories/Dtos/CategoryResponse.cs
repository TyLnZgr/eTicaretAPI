namespace ECommerce.Api.Features.Categories.Dtos;

public sealed record CategoryResponse(
    int Id,
    string Name,
    bool IsActive);
