namespace ECommerce.Api.Features.Payments.Outcomes;

public enum PaymentProcessingStatus
{
    Success,
    PaymentDeclined,
    InvalidRequest,
    OrderNotFound,
    OrderNotPayable,
    PaymentInProgress,
    IdempotencyConflict,
    ConcurrencyConflict
}
