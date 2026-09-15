using ECommerce.Api.Features.Categories.Dtos;
using ECommerce.Domain.Catalog;

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
