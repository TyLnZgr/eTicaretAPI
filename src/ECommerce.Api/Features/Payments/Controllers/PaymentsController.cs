using ECommerce.Api.Common.Authentication;
using ECommerce.Api.Features.Payments.Dtos;
using ECommerce.Api.Features.Payments.Mappings;
using ECommerce.Api.Features.Payments.Outcomes;
using ECommerce.Api.Features.Payments.Services;
using ECommerce.Api.Features.Payments.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Payments.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:int}/payments")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("{paymentId:guid}", Name = "GetPaymentById")]
    [EndpointSummary("Get a payment attempt by ID")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> GetByIdAsync(
        [FromRoute] int orderId,
        [FromRoute] Guid paymentId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var payment = await _paymentService.GetByIdAsync(
            orderId,
            paymentId,
            customerId,
            cancellationToken);

        if (payment is null)
        {
            return Problem(
                detail: $"Payment with ID {paymentId} was not found for order {orderId}.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(payment.ToResponse());
    }

    [HttpPost(Name = "ProcessOrderPayment")]
    [EndpointSummary("Process a payment for an order")]
    [EndpointDescription(
        "Processes a tokenized payment idempotently and moves the order from Pending to Paid when successful.")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PaymentResponse>(
        StatusCodes.Status402PaymentRequired)]
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
    public async Task<ActionResult<PaymentResponse>> ProcessAsync(
        [FromRoute] int orderId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = PaymentRequestValidator.ValidateProcess(
            idempotencyKey,
            request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _paymentService.ProcessAsync(
            orderId,
            customerId,
            idempotencyKey!.Trim(),
            request.PaymentMethodToken,
            cancellationToken);

        if (result.Status == PaymentProcessingStatus.OrderNotFound)
        {
            return Problem(
                detail: $"Order with ID {orderId} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (result.Status == PaymentProcessingStatus.OrderNotPayable)
        {
            return Conflict(
                "Only a pending order can be paid.");
        }

        if (result.Status == PaymentProcessingStatus.PaymentInProgress)
        {
            return Conflict(
                "A payment is already being processed for this order.");
        }

        if (result.Status == PaymentProcessingStatus.IdempotencyConflict)
        {
            return Conflict(
                "The Idempotency-Key was already used with a different payment request.");
        }

        if (result.Status == PaymentProcessingStatus.ConcurrencyConflict)
        {
            return Conflict(
                "The payment or order state changed during processing. Retry with the same Idempotency-Key.");
        }

        if (result.Status == PaymentProcessingStatus.InvalidRequest)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["payment"] = new[]
                        {
                            "The payment request is invalid."
                        }
                    }));
        }

        if (result.Payment is null)
        {
            return Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }

        var response = result.Payment.ToResponse();

        if (result.Status == PaymentProcessingStatus.PaymentDeclined)
        {
            return StatusCode(
                StatusCodes.Status402PaymentRequired,
                response);
        }

        if (result.WasReplay)
        {
            return Ok(response);
        }

        return CreatedAtRoute(
            "GetPaymentById",
            new
            {
                orderId,
                paymentId = result.Payment.Id
            },
            response);
    }

    private ObjectResult Conflict(string detail)
    {
        return Problem(
            detail: detail,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict");
    }
}
