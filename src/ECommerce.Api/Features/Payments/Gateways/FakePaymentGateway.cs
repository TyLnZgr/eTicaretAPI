using System.Collections.Concurrent;

namespace ECommerce.Api.Features.Payments.Gateways;

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly ConcurrentDictionary<string, PaymentGatewayResult>
        _resultsByIdempotencyKey = new();

    public string Name => "FakePay";

    public Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _resultsByIdempotencyKey.GetOrAdd(
            request.IdempotencyKey,
            _ => CreateResult(request));

        return Task.FromResult(result);
    }

    private static PaymentGatewayResult CreateResult(
        PaymentGatewayRequest request)
    {
        var providerPaymentId = $"fake_{request.PaymentId:N}";

        if (request.PaymentMethodToken == "tok_success")
        {
            return new PaymentGatewayResult(
                PaymentGatewayStatus.Succeeded,
                providerPaymentId);
        }

        return new PaymentGatewayResult(
            PaymentGatewayStatus.Declined,
            providerPaymentId,
            FailureCode: "payment_method_declined");
    }
}
