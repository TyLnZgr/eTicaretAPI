using ECommerce.Api.Common.Caching;
using ECommerce.Api.Common.RateLimiting;
using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Mappings;
using ECommerce.Application.Products.Outcomes;
using ECommerce.Application.Products.Services;
using ECommerce.Application.Products.Validation;
using ECommerce.Api.Common.Http;
using ECommerce.Api.Identity.Authorization;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Features.Products.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly CatalogOutputCache _catalogOutputCache;

    public ProductsController(
        IProductService productService,
        CatalogOutputCache catalogOutputCache)
    {
        _productService = productService;
        _catalogOutputCache = catalogOutputCache;
    }

    [HttpGet(Name = "GetProducts")]
    [OutputCache(PolicyName = CatalogOutputCache.ProductPolicyName)]
    [EndpointSummary("List products")]
    [EndpointDescription(
        "Returns a filtered, sorted, and paginated product list.")]
    [ProducesResponseType<PagedResult<ProductResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAllAsync(
        [FromQuery] ProductQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.ValidateQuery(queryParameters);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _productService.GetAllAsync(
            queryParameters,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [EndpointSummary("Get a product by ID")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        Response.Headers.ETag = EntityTagHeader.Format(product.Version);
        return Ok(product);
    }

    [HttpGet("{id:int}/stock-movements", Name = "GetProductStockMovements")]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("List product stock movements")]
    [EndpointDescription(
        "Returns the product's stock movement history, newest first.")]
    [ProducesResponseType<IReadOnlyList<StockMovementResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>>
        GetStockMovementsAsync(
            [FromRoute] int id,
            CancellationToken cancellationToken)
    {
        var movements = await _productService.GetStockMovementsAsync(
            id,
            cancellationToken);

        if (movements is null)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(movements);
    }

    [HttpPost(Name = "CreateProduct")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Create a product")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> CreateAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.ValidateCreate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _productService.CreateAsync(
            request.Name.Trim(),
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            request.IsActive,
            cancellationToken);

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return Problem(
                detail: $"Category with ID {request.CategoryId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Product is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        var response = result.Product.ToResponse();
        Response.Headers.ETag = EntityTagHeader.Format(response.Version);
        await _catalogOutputCache.EvictProductsAsync(cancellationToken);

        return CreatedAtRoute(
            "GetProductById",
            new { id = result.Product.Id },
            response);
    }

    [HttpPut("{id:int}", Name = "UpdateProduct")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Update a product")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status412PreconditionFailed,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status428PreconditionRequired,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        if (!Request.Headers.TryGetValue(
                HeaderNames.IfMatch,
                out var ifMatchValues))
        {
            return Problem(
                detail: "The If-Match header is required to update a product.",
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "Precondition Required",
                type: "https://www.rfc-editor.org/rfc/rfc6585#section-3");
        }

        if (!EntityTagHeader.TryParse(ifMatchValues, out var expectedVersion))
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [HeaderNames.IfMatch] = new[]
                        {
                            "If-Match must contain one strong product ETag."
                        }
                    }));
        }

        var result = await _productService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.Price,
            request.CategoryId,
            request.IsActive,
            expectedVersion,
            cancellationToken);

        if (result.Status == ProductMutationStatus.ProductNotFound)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == ProductMutationStatus.CategoryNotFound)
        {
            return Problem(
                detail: $"Category with ID {request.CategoryId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == ProductMutationStatus.ConcurrencyConflict)
        {
            return Problem(
                detail: "The product changed after it was retrieved. " +
                        "Get the product again and retry with the new ETag.",
                statusCode: StatusCodes.Status412PreconditionFailed,
                title: "Precondition Failed");
        }

        if (result.Product is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        var response = result.Product.ToResponse();
        Response.Headers.ETag = EntityTagHeader.Format(response.Version);
        await _catalogOutputCache.EvictProductsAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{id:int}/stock", Name = "AdjustProductStock")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Adjust product stock")]
    [EndpointDescription(
        "Adds or removes stock and records the reason as a movement.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdjustStockAsync(
        [FromRoute] int id,
        [FromBody] AdjustProductStockRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.ValidateStockAdjustment(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var status = await _productService.AdjustStockAsync(
            id,
            request.QuantityDelta,
            request.Reason.Trim(),
            cancellationToken);

        if (status == ProductStockAdjustmentStatus.Success)
        {
            await _catalogOutputCache.EvictProductsAsync(
                cancellationToken);
        }

        return status switch
        {
            ProductStockAdjustmentStatus.Success => NoContent(),

            ProductStockAdjustmentStatus.ProductNotFound => Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"),

            ProductStockAdjustmentStatus.InvalidQuantityDelta =>
                ValidationProblem(
                    new ValidationProblemDetails(
                        new Dictionary<string, string[]>
                        {
                            ["quantityDelta"] = new[]
                        {
                            "Quantity delta must be different from zero."
                        }
                        })),

            ProductStockAdjustmentStatus.InvalidReason =>
                ValidationProblem(
                    new ValidationProblemDetails(
                        new Dictionary<string, string[]>
                        {
                            ["reason"] = new[]
                        {
                            "Stock movement reason must be between 1 and 200 characters."
                        }
                        })),

            ProductStockAdjustmentStatus.InsufficientStock => Problem(
                detail: "The stock adjustment would result in a negative quantity.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict"),

            ProductStockAdjustmentStatus.StockLimitExceeded => Problem(
                detail: "The stock adjustment exceeds the supported stock limit.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict"),

            _ => Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error")
        };
    }

    [HttpDelete("{id:int}", Name = "DeleteProduct")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Soft-delete a product")]
    [EndpointDescription(
        "Marks a product as deleted while preserving its historical data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status412PreconditionFailed,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status428PreconditionRequired,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(
                HeaderNames.IfMatch,
                out var ifMatchValues))
        {
            return Problem(
                detail: "The If-Match header is required to delete a product.",
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "Precondition Required",
                type: "https://www.rfc-editor.org/rfc/rfc6585#section-3");
        }

        if (!EntityTagHeader.TryParse(ifMatchValues, out var expectedVersion))
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [HeaderNames.IfMatch] = new[]
                        {
                            "If-Match must contain one strong product ETag."
                        }
                    }));
        }

        var status = await _productService.DeleteAsync(
            id,
            expectedVersion,
            cancellationToken);

        if (status == ProductDeleteStatus.ProductNotFound)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (status == ProductDeleteStatus.ConcurrencyConflict)
        {
            return Problem(
                detail: "The product changed after it was retrieved. " +
                        "Get the product again and retry with the new ETag.",
                statusCode: StatusCodes.Status412PreconditionFailed,
                title: "Precondition Failed");
        }

        await _catalogOutputCache.EvictProductsAsync(cancellationToken);

        return NoContent();
    }
}
