using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Products.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("GetProducts");

        group.MapGet("/{id:int}", GetByIdAsync)
            .WithName("GetProductById");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CreateProduct");

        group.MapPut("/{id:int}", UpdateAsync)
            .WithName("UpdateProduct");

        group.MapDelete("/{id:int}", DeleteAsync)
            .WithName("DeleteProduct");

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] ProductQueryParameters queryParameters,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (queryParameters.CategoryId.HasValue &&
            queryParameters.CategoryId.Value <= 0)
        {
            return Results.BadRequest(new
            {
                message = "Category ID must be greater than zero."
            });
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MinPrice.Value < 0)
        {
            return Results.BadRequest(new
            {
                message = "Minimum price cannot be negative."
            });
        }

        if (queryParameters.MaxPrice.HasValue &&
            queryParameters.MaxPrice.Value < 0)
        {
            return Results.BadRequest(new
            {
                message = "Maximum price cannot be negative."
            });
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MaxPrice.HasValue &&
            queryParameters.MinPrice.Value > queryParameters.MaxPrice.Value)
        {
            return Results.BadRequest(new
            {
                message = "Minimum price cannot be greater than maximum price."
            });
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
            return Results.BadRequest(new
            {
                message = "Sort field must be id, name, price, or stockQuantity."
            });
        }

        if (sortDirection != "asc" &&
            sortDirection != "desc")
        {
            return Results.BadRequest(new
            {
                message = "Sort direction must be asc or desc."
            });
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;

        if (page < 1)
        {
            return Results.BadRequest(new
            {
                message = "Page must be greater than zero."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Results.BadRequest(new
            {
                message = "Page size must be between 1 and 100."
            });
        }

        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            return Results.BadRequest(new
            {
                message = "Requested page is too large."
            });
        }

        var result = await productService.GetAllAsync(
            queryParameters,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return Results.NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        return Results.Ok(product);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (request.Name.Trim().Length > 200)
        {
            return Results.BadRequest(new
            {
                message = "Product name cannot exceed 200 characters."
            });
        }

        if (request.Price <= 0)
        {
            return Results.BadRequest(new
            {
                message = "Product price must be greater than zero."
            });
        }

        if (request.StockQuantity < 0)
        {
            return Results.BadRequest(new
            {
                message = "Product stock quantity cannot be negative."
            });
        }

        if (request.CategoryId <= 0)
        {
            return Results.BadRequest(new
            {
                message = "A valid category ID is required."
            });
        }

        var result = await productService.CreateAsync(
            request.Name.Trim(),
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            request.IsActive,
            cancellationToken);

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return Results.NotFound(new
            {
                message = $"Category with ID {request.CategoryId} was not found."
            });
        }

        if (result.Product is null)
        {
            return Results.Problem(
                "Product creation completed without a product.");
        }

        return Results.Created(
            $"/api/products/{result.Product.Id}",
            ToResponse(result.Product));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateProductRequest request,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (request.Name.Trim().Length > 200)
        {
            return Results.BadRequest(new
            {
                message = "Product name cannot exceed 200 characters."
            });
        }

        if (request.Price <= 0)
        {
            return Results.BadRequest(new
            {
                message = "Product price must be greater than zero."
            });
        }

        if (request.StockQuantity < 0)
        {
            return Results.BadRequest(new
            {
                message = "Product stock quantity cannot be negative."
            });
        }

        if (request.CategoryId <= 0)
        {
            return Results.BadRequest(new
            {
                message = "A valid category ID is required."
            });
        }

        var result = await productService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            request.IsActive,
            cancellationToken);

        if (result.Status == ProductMutationStatus.ProductNotFound)
        {
            return Results.NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return Results.NotFound(new
            {
                message = $"Category with ID {request.CategoryId} was not found."
            });
        }

        if (result.Product is null)
        {
            return Results.Problem(
                "Product update completed without a product.");
        }

        return Results.Ok(ToResponse(result.Product));
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        var wasDeleted = await productService.DeleteAsync(
            id,
            cancellationToken);

        if (!wasDeleted)
        {
            return Results.NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        return Results.NoContent();
    }

    private static ProductResponse ToResponse(Product product)
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
