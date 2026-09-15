using ECommerce.Application.Payments.Dtos;
using ECommerce.Domain.Payments;

namespace ECommerce.Application.Payments.Validation;

public static class PaymentRequestValidator
{
    public const int PaymentMethodTokenMaxLength = 200;

    public static Dictionary<string, string[]> ValidateProcess(
        string? idempotencyKey,
        ProcessPaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Payment.IsIdempotencyKeyValid(idempotencyKey))
        {
            errors["Idempotency-Key"] = new[]
            {
                "Idempotency-Key must be 8 to 100 characters and contain only letters, digits, '-', '_', '.', or ':'."
            };
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodToken) ||
            request.PaymentMethodToken.Length > PaymentMethodTokenMaxLength ||
            request.PaymentMethodToken.Any(char.IsWhiteSpace))
        {
            errors["paymentMethodToken"] = new[]
            {
                "A valid payment method token is required."
            };
        }

        return errors;
    }
}
