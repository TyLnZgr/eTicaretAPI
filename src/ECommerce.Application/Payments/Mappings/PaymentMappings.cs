using ECommerce.Application.Payments.Dtos;
using ECommerce.Domain.Payments;

namespace ECommerce.Application.Payments.Mappings;

public static class PaymentMappings
{
    public static PaymentResponse ToResponse(this Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.Provider,
            payment.ProviderPaymentId,
            payment.FailureCode,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc);
    }
}
