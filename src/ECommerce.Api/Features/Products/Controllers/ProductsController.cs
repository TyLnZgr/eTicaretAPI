using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Mappings;
using ECommerce.Api.Features.Products.Outcomes;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Features.Products.Validation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Products.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet(Name = "GetProducts")]
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

        return Ok(product);
    }

    [HttpGet("{id:int}/stock-movements", Name = "GetProductStockMovements")]
    [EndpointSummary("List product stock movements")]
    [EndpointDescription(
        "Returns the product's stock movement history, newest first.")]
    [ProducesResponseType<IReadOnlyList<StockMovementResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
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

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = result.Product.Id },
            response);
    }

    [HttpPut("{id:int}", Name = "UpdateProduct")]
    [EndpointSummary("Update a product")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
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

        var result = await _productService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.Price,
            request.CategoryId,
            request.IsActive,
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

        if (result.Product is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        return Ok(result.Product.ToResponse());
    }

    [HttpPatch("{id:int}/stock", Name = "AdjustProductStock")]
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
    [EndpointSummary("Delete a product")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var wasDeleted = await _productService.DeleteAsync(
            id,
            cancellationToken);

        if (!wasDeleted)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return NoContent();
    }
}
