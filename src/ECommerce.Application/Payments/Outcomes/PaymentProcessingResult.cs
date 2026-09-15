using ECommerce.Domain.Payments;

namespace ECommerce.Application.Payments.Outcomes;

public sealed record PaymentProcessingResult(
    PaymentProcessingStatus Status,
    Payment? Payment = null,
    bool WasReplay = false);
