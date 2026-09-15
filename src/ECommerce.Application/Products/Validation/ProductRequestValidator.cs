using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Catalog;

namespace ECommerce.Application.Products.Validation;

public static class ProductRequestValidator
{
    public static Dictionary<string, string[]> ValidateQuery(
        ProductQueryParameters queryParameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (queryParameters.CategoryId.HasValue &&
            queryParameters.CategoryId.Value <= 0)
        {
            errors["categoryId"] = new[]
            {
                "Category ID must be greater than zero."
            };
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MinPrice.Value < 0)
        {
            errors["minPrice"] = new[]
            {
                "Minimum price cannot be negative."
            };
        }

        if (queryParameters.MaxPrice.HasValue &&
            queryParameters.MaxPrice.Value < 0)
        {
            errors["maxPrice"] = new[]
            {
                "Maximum price cannot be negative."
            };
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MaxPrice.HasValue &&
            queryParameters.MinPrice.Value > queryParameters.MaxPrice.Value)
        {
            errors["priceRange"] = new[]
            {
                "Minimum price cannot be greater than maximum price."
            };
        }

        var sortBy = string.IsNullOrWhiteSpace(queryParameters.SortBy)
            ? "id"
            : queryParameters.SortBy.Trim().ToLowerInvariant();

        var sortDirection =
            string.IsNullOrWhiteSpace(queryParameters.SortDirection)
                ? "asc"
                : queryParameters.SortDirection.Trim().ToLowerInvariant();

        if (sortBy != "id" &&
            sortBy != "name" &&
            sortBy != "price" &&
            sortBy != "stockquantity")
        {
            errors["sortBy"] = new[]
            {
                "Sort field must be id, name, price, or stockQuantity."
            };
        }

        if (sortDirection != "asc" &&
            sortDirection != "desc")
        {
            errors["sortDirection"] = new[]
            {
                "Sort direction must be asc or desc."
            };
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;

        if (page < 1)
        {
            errors["page"] = new[]
            {
                "Page must be greater than zero."
            };
        }

        if (pageSize < 1 || pageSize > 100)
        {
            errors["pageSize"] = new[]
            {
                "Page size must be between 1 and 100."
            };
        }

        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            errors["page"] = new[]
            {
                "Requested page is too large."
            };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCreate(
        CreateProductRequest request)
    {
        var errors = ValidateDetails(
            request.Name,
            request.Price,
            request.CategoryId);

        if (request.StockQuantity < 0)
        {
            errors["stockQuantity"] = new[]
            {
                "Product stock quantity cannot be negative."
            };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(
        UpdateProductRequest request)
    {
        return ValidateDetails(
            request.Name,
            request.Price,
            request.CategoryId);
    }

    public static Dictionary<string, string[]> ValidateStockAdjustment(
        AdjustProductStockRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.QuantityDelta == 0)
        {
            errors["quantityDelta"] = new[]
            {
                "Quantity delta must be different from zero."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            errors["reason"] = new[]
            {
                "Stock movement reason is required."
            };
        }
        else if (request.Reason.Trim().Length >
                 StockMovement.MaxReasonLength)
        {
            errors["reason"] = new[]
            {
                "Stock movement reason cannot exceed 200 characters."
            };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateDetails(
        string name,
        decimal price,
        int categoryId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = new[]
            {
                "Product name is required."
            };
        }
        else if (name.Trim().Length > Product.MaxNameLength)
        {
            errors["name"] = new[]
            {
                "Product name cannot exceed 200 characters."
            };
        }

        if (price <= 0)
        {
            errors["price"] = new[]
            {
                "Product price must be greater than zero."
            };
        }

        if (categoryId <= 0)
        {
            errors["categoryId"] = new[]
            {
                "A valid category ID is required."
            };
        }

        return errors;
    }
}
