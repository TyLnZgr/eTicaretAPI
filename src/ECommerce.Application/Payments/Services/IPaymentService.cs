using ECommerce.Application.Payments.Outcomes;
using ECommerce.Domain.Payments;

namespace ECommerce.Application.Payments.Services;

public interface IPaymentService
{
    Task<Payment?> GetByIdAsync(
        int orderId,
        Guid paymentId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<PaymentProcessingResult> ProcessAsync(
        int orderId,
        Guid customerId,
        string idempotencyKey,
        string paymentMethodToken,
        CancellationToken cancellationToken = default);
}
