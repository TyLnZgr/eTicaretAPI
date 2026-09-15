using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Catalog;

namespace ECommerce.Application.Products.Mappings;

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
            product.Category.Name,
            product.Version);
    }
}
