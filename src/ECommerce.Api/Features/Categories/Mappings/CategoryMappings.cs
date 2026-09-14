using ECommerce.Api.Features.Categories.Dtos;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Categories.Mappings;

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.IsActive);
    }
}
