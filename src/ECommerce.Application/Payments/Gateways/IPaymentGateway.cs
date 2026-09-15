namespace ECommerce.Application.Payments.Gateways;

public interface IPaymentGateway
{
    string Name { get; }

    Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);
}
