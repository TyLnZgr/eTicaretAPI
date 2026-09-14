using ECommerce.Api.Common.Authentication;
using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Notifications.Dtos;
using ECommerce.Api.Features.Notifications.Services;
using ECommerce.Api.Features.Notifications.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Notifications.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet(Name = "GetNotifications")]
    [EndpointSummary("List the current customer's notifications")]
    [EndpointDescription(
        "Returns paginated notifications, newest first.")]
    [ProducesResponseType<PagedResult<NotificationResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResult<NotificationResponse>>>
        GetAllAsync(
            [FromQuery] NotificationQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = NotificationRequestValidator.ValidateQuery(
            queryParameters);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        return Ok(await _notificationService.GetAllAsync(
            customerId,
            queryParameters,
            cancellationToken));
    }

    [HttpPatch("{id:guid}/read", Name = "MarkNotificationAsRead")]
    [EndpointSummary("Mark a notification as read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<IActionResult> MarkAsReadAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var wasFound = await _notificationService.MarkAsReadAsync(
            id,
            customerId,
            cancellationToken);

        if (!wasFound)
        {
            return Problem(
                detail: $"Notification with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return NoContent();
    }
}
