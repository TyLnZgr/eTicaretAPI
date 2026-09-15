using ECommerce.Api.Common.Authentication;
using ECommerce.Api.Common.Caching;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Mappings;
using ECommerce.Application.Carts.Outcomes;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Carts.Validation;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Mappings;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Carts.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IOrderPlacementService _orderPlacementService;
    private readonly CatalogOutputCache _catalogOutputCache;

    public CartController(
        ICartService cartService,
        IOrderPlacementService orderPlacementService,
        CatalogOutputCache catalogOutputCache)
    {
        _cartService = cartService;
        _orderPlacementService = orderPlacementService;
        _catalogOutputCache = catalogOutputCache;
    }

    [HttpGet(Name = "GetCart")]
    [EndpointSummary("Get the current customer's cart")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> GetAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var cart = await _cartService.GetAsync(
            customerId,
            cancellationToken);

        return Ok(cart?.ToResponse() ?? CartResponse.Empty);
    }

    [HttpPut("items/{productId:int}", Name = "SetCartItemQuantity")]
    [EndpointSummary("Set a product quantity in the current customer's cart")]
    [EndpointDescription(
        "Creates or replaces the cart item's absolute quantity without reserving stock.")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
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
    public async Task<ActionResult<CartResponse>> SetItemQuantityAsync(
        [FromRoute] int productId,
        [FromBody] SetCartItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = CartRequestValidator.ValidateSetQuantity(
            productId,
            request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _cartService.SetItemQuantityAsync(
            customerId,
            productId,
            request.Quantity,
            cancellationToken);

        if (result.Status == CartMutationStatus.InvalidRequest)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["cart"] = new[]
                        {
                            "The cart item request is invalid."
                        }
                    }));
        }

        if (result.Status == CartMutationStatus.CustomerNotFound)
        {
            return Unauthorized();
        }

        if (result.Status == CartMutationStatus.ProductNotFound)
        {
            return Problem(
                detail: $"Product with ID {productId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == CartMutationStatus.ProductInactive)
        {
            return Problem(
                detail: $"Product with ID {productId} is not active.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == CartMutationStatus.InsufficientStock)
        {
            return Problem(
                detail: $"Product with ID {productId} has only {result.AvailableStock} units available.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Cart is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        return Ok(result.Cart.ToResponse());
    }

    [HttpDelete("items/{productId:int}", Name = "RemoveCartItem")]
    [EndpointSummary("Remove a product from the current customer's cart")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<IActionResult> RemoveItemAsync(
        [FromRoute] int productId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        if (productId <= 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["productId"] = new[]
                        {
                            "A valid product ID is required."
                        }
                    }));
        }

        var status = await _cartService.RemoveItemAsync(
            customerId,
            productId,
            cancellationToken);

        if (status == CartItemRemovalStatus.ItemNotFound)
        {
            return Problem(
                detail: $"Product with ID {productId} was not found in the cart.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return NoContent();
    }

    [HttpDelete(Name = "ClearCart")]
    [EndpointSummary("Remove all items from the current customer's cart")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        await _cartService.ClearAsync(
            customerId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("checkout", Name = "CheckoutCart")]
    [EndpointSummary("Create an order from the current customer's cart")]
    [EndpointDescription(
        "Idempotently revalidates products and stock, creates the order, decreases stock, records stock movements, and removes the cart atomically.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
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
    public async Task<ActionResult<OrderResponse>> CheckoutAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CheckoutCartRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = CartRequestValidator.ValidateCheckout(
            idempotencyKey,
            request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _orderPlacementService.CheckoutCartAsync(
            customerId,
            idempotencyKey!.Trim(),
            request.AddressId,
            cancellationToken);

        if (result.Status == OrderCreationStatus.CustomerNotFound)
        {
            return Unauthorized();
        }

        if (result.Status == OrderCreationStatus.CartEmpty)
        {
            return Problem(
                detail: "The cart is empty.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status ==
            OrderCreationStatus.ShippingAddressNotFound)
        {
            return Problem(
                detail: $"Address with ID {result.AddressId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == OrderCreationStatus.ProductNotFound)
        {
            return Problem(
                detail: $"Product with ID {result.ProductId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == OrderCreationStatus.ProductInactive)
        {
            return Problem(
                detail: $"Product with ID {result.ProductId} is not active.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderCreationStatus.InsufficientStock)
        {
            return Problem(
                detail: $"Product with ID {result.ProductId} does not have sufficient stock.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderCreationStatus.ConcurrencyConflict)
        {
            return Problem(
                detail: "The cart was changed by another request. Reload it and try again.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderCreationStatus.IdempotencyConflict)
        {
            return Problem(
                detail: "The Idempotency-Key was already used with a different order request.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderCreationStatus.InvalidRequest)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["cart"] = new[]
                        {
                            "The cart cannot be checked out."
                        }
                    }));
        }

        if (result.Order is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        var response = result.Order.ToResponse();

        if (result.WasReplay)
        {
            return Ok(response);
        }

        await _catalogOutputCache.EvictProductsAsync(cancellationToken);

        return CreatedAtRoute(
            "GetOrderById",
            new { id = result.Order.Id },
            response);
    }
}
