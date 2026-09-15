using ECommerce.Api.Common.Http;
using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Outcomes;
using ECommerce.Application.Products.Services;
using ECommerce.Domain.Catalog;
using Microsoft.Net.Http.Headers;

namespace ECommerce.Api.Features.Products.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("GetProducts")
            .WithSummary("List products")
            .WithDescription(
                "Returns a filtered, sorted, and paginated product list.")
            .Produces<PagedResult<ProductResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{id:int}", GetByIdAsync)
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:int}/stock-movements", GetStockMovementsAsync)
            .WithName("GetProductStockMovements")
            .WithSummary("List product stock movements")
            .WithDescription(
                "Returns the product's stock movement history, newest first.")
            .Produces<IReadOnlyList<StockMovementResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CreateProduct")
            .WithSummary("Create a product")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id:int}", UpdateAsync)
            .WithName("UpdateProduct")
            .WithSummary("Update a product")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{id:int}/stock", AdjustStockAsync)
            .WithName("AdjustProductStock")
            .WithSummary("Adjust product stock")
            .WithDescription(
                "Adds or removes stock and records the reason as a movement.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id:int}", DeleteAsync)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
            return ApiProblemResults.Validation(
                "categoryId",
                "Category ID must be greater than zero.");
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MinPrice.Value < 0)
        {
            return ApiProblemResults.Validation(
                "minPrice",
                "Minimum price cannot be negative.");
        }

        if (queryParameters.MaxPrice.HasValue &&
            queryParameters.MaxPrice.Value < 0)
        {
            return ApiProblemResults.Validation(
                "maxPrice",
                "Maximum price cannot be negative.");
        }

        if (queryParameters.MinPrice.HasValue &&
            queryParameters.MaxPrice.HasValue &&
            queryParameters.MinPrice.Value > queryParameters.MaxPrice.Value)
        {
            return ApiProblemResults.Validation(
                "priceRange",
                "Minimum price cannot be greater than maximum price.");
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
            return ApiProblemResults.Validation(
                "sortBy",
                "Sort field must be id, name, price, or stockQuantity.");
        }

        if (sortDirection != "asc" &&
            sortDirection != "desc")
        {
            return ApiProblemResults.Validation(
                "sortDirection",
                "Sort direction must be asc or desc.");
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;

        if (page < 1)
        {
            return ApiProblemResults.Validation(
                "page",
                "Page must be greater than zero.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return ApiProblemResults.Validation(
                "pageSize",
                "Page size must be between 1 and 100.");
        }

        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            return ApiProblemResults.Validation(
                "page",
                "Requested page is too large.");
        }

        var result = await productService.GetAllAsync(
            queryParameters,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IProductService productService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ApiProblemResults.NotFound(
                $"Product with ID {id} was not found.");
        }

        httpContext.Response.Headers.ETag =
            EntityTagHeader.Format(product.Version);

        return Results.Ok(product);
    }

    private static async Task<IResult> GetStockMovementsAsync(
        int id,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        var movements = await productService.GetStockMovementsAsync(
            id,
            cancellationToken);

        if (movements is null)
        {
            return ApiProblemResults.NotFound(
                $"Product with ID {id} was not found.");
        }

        return Results.Ok(movements);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        IProductService productService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateProductDetailsRequest(
            request.Name,
            request.Price,
            request.CategoryId);

        if (validationResult is not null)
        {
            return validationResult;
        }

        if (request.StockQuantity < 0)
        {
            return ApiProblemResults.Validation(
                "stockQuantity",
                "Product stock quantity cannot be negative.");
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
            return ApiProblemResults.NotFound(
                $"Category with ID {request.CategoryId} was not found.");
        }

        if (result.Product is null)
        {
            return ApiProblemResults.InternalServerError();
        }

        httpContext.Response.Headers.ETag =
            EntityTagHeader.Format(result.Product.Version);

        return Results.Created(
            $"/api/products/{result.Product.Id}",
            ToResponse(result.Product));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateProductRequest request,
        IProductService productService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateProductDetailsRequest(
            request.Name,
            request.Price,
            request.CategoryId);

        if (validationResult is not null)
        {
            return validationResult;
        }

        if (!httpContext.Request.Headers.TryGetValue(
                HeaderNames.IfMatch,
                out var ifMatchValues))
        {
            return ApiProblemResults.PreconditionRequired(
                "The If-Match header is required to update a product.");
        }

        if (!EntityTagHeader.TryParse(ifMatchValues, out var expectedVersion))
        {
            return ApiProblemResults.Validation(
                HeaderNames.IfMatch,
                "If-Match must contain one strong product ETag.");
        }

        var result = await productService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.Price,
            request.CategoryId,
            request.IsActive,
            expectedVersion,
            cancellationToken);

        if (result.Status == ProductMutationStatus.ProductNotFound)
        {
            return ApiProblemResults.NotFound(
                $"Product with ID {id} was not found.");
        }

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return ApiProblemResults.NotFound(
                $"Category with ID {request.CategoryId} was not found.");
        }

        if (result.Status == ProductMutationStatus.ConcurrencyConflict)
        {
            return ApiProblemResults.PreconditionFailed(
                "The product changed after it was retrieved. " +
                "Get the product again and retry with the new ETag.");
        }

        if (result.Product is null)
        {
            return ApiProblemResults.InternalServerError();
        }

        var response = ToResponse(result.Product);
        httpContext.Response.Headers.ETag =
            EntityTagHeader.Format(response.Version);

        return Results.Ok(response);
    }

    private static async Task<IResult> AdjustStockAsync(
        int id,
        AdjustProductStockRequest request,
        IProductService productService,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            return ApiProblemResults.Validation(
                "quantityDelta",
                "Quantity delta must be different from zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApiProblemResults.Validation(
                "reason",
                "Stock movement reason is required.");
        }

        if (request.Reason.Trim().Length > 200)
        {
            return ApiProblemResults.Validation(
                "reason",
                "Stock movement reason cannot exceed 200 characters.");
        }

        var status = await productService.AdjustStockAsync(
            id,
            request.QuantityDelta,
            request.Reason.Trim(),
            cancellationToken);

        return status switch
        {
            ProductStockAdjustmentStatus.Success =>
                Results.NoContent(),

            ProductStockAdjustmentStatus.ProductNotFound =>
                ApiProblemResults.NotFound(
                    $"Product with ID {id} was not found."),

            ProductStockAdjustmentStatus.InvalidQuantityDelta =>
                ApiProblemResults.Validation(
                    "quantityDelta",
                    "Quantity delta must be different from zero."),

            ProductStockAdjustmentStatus.InvalidReason =>
                ApiProblemResults.Validation(
                    "reason",
                    "Stock movement reason must be between 1 and 200 characters."),

            ProductStockAdjustmentStatus.InsufficientStock =>
                ApiProblemResults.Conflict(
                    "The stock adjustment would result in a negative quantity."),

            ProductStockAdjustmentStatus.StockLimitExceeded =>
                ApiProblemResults.Conflict(
                    "The stock adjustment exceeds the supported stock limit."),

            _ => ApiProblemResults.InternalServerError()
        };
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
            return ApiProblemResults.NotFound(
                $"Product with ID {id} was not found.");
        }

        return Results.NoContent();
    }

    private static IResult? ValidateProductDetailsRequest(
        string name,
        decimal price,
        int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApiProblemResults.Validation(
                "name",
                "Product name is required.");
        }

        if (name.Trim().Length > 200)
        {
            return ApiProblemResults.Validation(
                "name",
                "Product name cannot exceed 200 characters.");
        }

        if (price <= 0)
        {
            return ApiProblemResults.Validation(
                "price",
                "Product price must be greater than zero.");
        }

        if (categoryId <= 0)
        {
            return ApiProblemResults.Validation(
                "categoryId",
                "A valid category ID is required.");
        }

        return null;
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
            product.Category.Name,
            product.Version);
    }
}
