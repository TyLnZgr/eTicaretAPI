using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Mappings;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Orders.Validation;
using ECommerce.Api.Common.RateLimiting;
using ECommerce.Api.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Features.Orders.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.ManageOrders)]
[Route("api/admin/orders")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet(Name = "GetOrdersAsAdministrator")]
    [EndpointSummary("List orders as an administrator")]
    [EndpointDescription(
        "Returns filtered and paginated orders across all customers, newest first.")]
    [ProducesResponseType<PagedResult<AdminOrderResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminOrderResponse>>>
        GetAllAsync(
            [FromQuery] AdminOrderQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.ValidateAdminQuery(
            queryParameters);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result =
            await _orderService.GetAllAsAdministratorAsync(
                queryParameters,
                cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:int}/status", Name = "UpdateOrderStatusAsAdministrator")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [EndpointSummary("Update an order status as an administrator")]
    [EndpointDescription(
        "Applies a valid order state transition without customer ownership filtering.")]
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

        var result =
            await _orderService.UpdateStatusAsAdministratorAsync(
                id,
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
