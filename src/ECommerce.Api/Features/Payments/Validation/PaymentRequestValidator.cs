using ECommerce.Api.Features.Payments.Dtos;

namespace ECommerce.Api.Features.Payments.Validation;

public static class PaymentRequestValidator
{
    public static Dictionary<string, string[]> ValidateProcess(
        string? idempotencyKey,
        ProcessPaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedKey = idempotencyKey?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedKey) ||
            normalizedKey.Length < 8 ||
            normalizedKey.Length > 100 ||
            normalizedKey.Any(character =>
                !(char.IsLetterOrDigit(character) ||
                  character is '-' or '_' or '.' or ':')))
        {
            errors["Idempotency-Key"] = new[]
            {
                "Idempotency-Key must be 8 to 100 characters and contain only letters, digits, '-', '_', '.', or ':'."
            };
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodToken) ||
            request.PaymentMethodToken.Length > 200 ||
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
