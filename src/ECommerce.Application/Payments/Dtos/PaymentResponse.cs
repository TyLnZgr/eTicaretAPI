namespace ECommerce.Application.Payments.Dtos;

public sealed record PaymentResponse(
    Guid Id,
    int OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? ProviderPaymentId,
    string? FailureCode,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
