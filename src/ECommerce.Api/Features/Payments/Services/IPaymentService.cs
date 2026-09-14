using ECommerce.Api.Features.Payments.Outcomes;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Payments.Services;

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
