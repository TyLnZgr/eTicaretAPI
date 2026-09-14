using ECommerce.Api.Common.Authentication;
using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Features.Orders.Mappings;
using ECommerce.Api.Features.Orders.Outcomes;
using ECommerce.Api.Features.Orders.Services;
using ECommerce.Api.Features.Orders.Validation;
using ECommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Orders.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet(Name = "GetOrders")]
    [EndpointSummary("List orders")]
    [EndpointDescription(
        "Returns filtered and paginated orders, newest first.")]
    [ProducesResponseType<PagedResult<OrderResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetAllAsync(
        [FromQuery] OrderQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = OrderRequestValidator.ValidateQuery(queryParameters);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _orderService.GetAllAsync(
            customerId,
            queryParameters,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}", Name = "GetOrderById")]
    [EndpointSummary("Get an order by ID")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var order = await _orderService.GetByIdAsync(
            id,
            customerId,
            cancellationToken);

        if (order is null)
        {
            return Problem(
                detail: $"Order with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(order.ToResponse());
    }

    [HttpPost(Name = "CreateOrder")]
    [EndpointSummary("Create an order")]
    [EndpointDescription(
        "Creates an order, decreases stock, and records stock movements atomically.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
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
        StatusCodes.Status403Forbidden,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> CreateAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = OrderRequestValidator.ValidateCreate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _orderService.CreateAsync(
            customerId,
            request.Items,
            cancellationToken);

        if (result.Status == OrderCreationStatus.CustomerNotFound)
        {
            return Unauthorized();
        }

        if (result.Status == OrderCreationStatus.InvalidRequest)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["order"] = new[]
                        {
                            "The order request is invalid."
                        }
                    }));
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

        if (result.Order is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        var response = result.Order.ToResponse();

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = result.Order.Id },
            response);
    }

    [HttpPatch("{id:int}/status", Name = "UpdateOrderStatus")]
    [EndpointSummary("Update an order status")]
    [EndpointDescription(
        "Applies a valid order state transition and restores stock when a pending order is cancelled.")]
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
    public async Task<ActionResult<OrderResponse>> UpdateStatusAsync(
        [FromRoute] int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        if (!OrderRequestValidator.TryParseStatus(request, out var newStatus))
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = new[]
                        {
                            "Status must be Pending, Paid, Shipped, Completed, or Cancelled."
                        }
                    }));
        }

        if (newStatus != OrderStatus.Cancelled)
        {
            return Problem(
                detail: "Customers can only cancel their own pending orders.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden");
        }

        var result = await _orderService.UpdateStatusAsync(
            id,
            customerId,
            newStatus,
            cancellationToken);

        if (result.Status == OrderStatusUpdateStatus.OrderNotFound)
        {
            return Problem(
                detail: $"Order with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == OrderStatusUpdateStatus.InvalidTransition)
        {
            return Problem(
                detail: $"Order status cannot transition from {result.CurrentStatus} to {newStatus}.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderStatusUpdateStatus.ConcurrencyConflict)
        {
            return Problem(
                detail: "The order was changed by another request. Reload it and try again.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Status == OrderStatusUpdateStatus.StockLimitExceeded)
        {
            return Problem(
                detail: $"Stock for product with ID {result.ProductId} cannot be restored because it would exceed the supported limit.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Order is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        return Ok(result.Order.ToResponse());
    }
}
