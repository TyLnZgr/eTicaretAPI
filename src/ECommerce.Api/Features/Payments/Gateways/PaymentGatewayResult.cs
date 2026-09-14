namespace ECommerce.Api.Features.Payments.Gateways;

public sealed record PaymentGatewayResult(
    PaymentGatewayStatus Status,
    string? ProviderPaymentId = null,
    string? FailureCode = null);
