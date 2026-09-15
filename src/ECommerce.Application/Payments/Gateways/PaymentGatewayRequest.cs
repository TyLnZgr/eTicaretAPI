namespace ECommerce.Application.Payments.Gateways;

public sealed record PaymentGatewayRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string PaymentMethodToken,
    string IdempotencyKey);
