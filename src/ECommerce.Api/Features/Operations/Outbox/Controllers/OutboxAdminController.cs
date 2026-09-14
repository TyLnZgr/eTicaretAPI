using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Operations.Outbox.Dtos;
using ECommerce.Api.Features.Operations.Outbox.Outcomes;
using ECommerce.Api.Features.Operations.Outbox.Services;
using ECommerce.Api.Features.Operations.Outbox.Validation;
using ECommerce.Api.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Operations.Outbox.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.ManageOperations)]
[Route("api/admin/outbox")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class OutboxAdminController : ControllerBase
{
    private readonly IOutboxAdministrationService _outboxService;

    public OutboxAdminController(
        IOutboxAdministrationService outboxService)
    {
        _outboxService = outboxService;
    }

    [HttpGet(Name = "GetOutboxMessages")]
    [EndpointSummary("List Outbox messages")]
    [EndpointDescription(
        "Returns operational Outbox metadata without event payloads.")]
    [ProducesResponseType<PagedResult<OutboxMessageResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<PagedResult<OutboxMessageResponse>>>
        GetAllAsync(
            [FromQuery] OutboxMessageQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        var errors = OutboxAdministrationValidator.ValidateQuery(
            queryParameters);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        return Ok(await _outboxService.GetAllAsync(
            queryParameters,
            cancellationToken));
    }

    [HttpPost("{id:guid}/retry", Name = "RetryOutboxMessage")]
    [EndpointSummary("Retry a dead-lettered Outbox message")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    public async Task<IActionResult> RetryAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var status = await _outboxService.RetryAsync(
            id,
            cancellationToken);

        if (status == OutboxRetryStatus.MessageNotFound)
        {
            return Problem(
                detail: $"Outbox message with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (status == OutboxRetryStatus.MessageNotDeadLettered)
        {
            return Problem(
                detail: "Only a dead-lettered Outbox message can be retried manually.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        return NoContent();
    }
}
