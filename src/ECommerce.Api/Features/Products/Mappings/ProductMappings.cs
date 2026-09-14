using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Products.Mappings;

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity,
            product.IsActive,
            product.CategoryId,
            product.Category.Name);
    }
}
