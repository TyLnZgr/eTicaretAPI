namespace ECommerce.Api.Features.Payments.Gateways;

public interface IPaymentGateway
{
    string Name { get; }

    Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);
}
