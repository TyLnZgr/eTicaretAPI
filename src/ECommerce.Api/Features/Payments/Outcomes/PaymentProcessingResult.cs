using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Payments.Outcomes;

public sealed record PaymentProcessingResult(
    PaymentProcessingStatus Status,
    Payment? Payment = null,
    bool WasReplay = false);
