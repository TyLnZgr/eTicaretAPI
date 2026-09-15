using ECommerce.Domain.Orders;

namespace ECommerce.Domain.Payments;

public sealed class Payment
{
    public const int IdempotencyKeyMinLength = 8;
    public const int IdempotencyKeyMaxLength = 100;
    public const int RequestFingerprintLength = 64;
    public const int CurrencyLength = 3;
    public const int ProviderMaxLength = 100;
    public const int ProviderPaymentIdMaxLength = 200;
    public const int FailureCodeMaxLength = 100;

    private Payment()
    {
    }

    public Payment(
        int orderId,
        string idempotencyKey,
        string requestFingerprint,
        decimal amount,
        string currency,
        string provider,
        DateTime createdAtUtc)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderId),
                "A valid order ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        RequestFingerprint = NormalizeRequestFingerprint(
            requestFingerprint);
        Amount = amount;
        Currency = NormalizeCurrency(currency);
        Provider = NormalizeRequired(
            provider,
            ProviderMaxLength,
            "Payment provider",
            nameof(provider));
        Status = PaymentStatus.Processing;
        CreatedAtUtc = NormalizeUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public int OrderId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderPaymentId { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public Order Order { get; private set; } = null!;

    public void MarkSucceeded(
        string providerPaymentId,
        DateTime updatedAtUtc)
    {
        EnsureProcessing();

        var normalizedProviderPaymentId = NormalizeRequired(
            providerPaymentId,
            ProviderPaymentIdMaxLength,
            "Provider payment ID",
            nameof(providerPaymentId));

        Status = PaymentStatus.Succeeded;
        ProviderPaymentId = normalizedProviderPaymentId;
        FailureCode = null;
        UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
    }

    public void MarkFailed(
        string failureCode,
        string? providerPaymentId,
        DateTime updatedAtUtc)
    {
        EnsureProcessing();

        var normalizedFailureCode = NormalizeRequired(
            failureCode,
            FailureCodeMaxLength,
            "Failure code",
            nameof(failureCode));
        var normalizedProviderPaymentId = NormalizeOptional(
            providerPaymentId,
            ProviderPaymentIdMaxLength,
            "Provider payment ID",
            nameof(providerPaymentId));

        Status = PaymentStatus.Failed;
        ProviderPaymentId = normalizedProviderPaymentId;
        FailureCode = normalizedFailureCode;
        UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
    }

    public static bool IsIdempotencyKeyValid(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return false;
        }

        var normalizedKey = idempotencyKey.Trim();

        return normalizedKey.Length >= IdempotencyKeyMinLength &&
               normalizedKey.Length <= IdempotencyKeyMaxLength &&
               normalizedKey.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character is '-' or '_' or '.' or ':');
    }

    private void EnsureProcessing()
    {
        if (Status != PaymentStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only a processing payment can be finalized.");
        }
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        if (!IsIdempotencyKeyValid(idempotencyKey))
        {
            throw new ArgumentException(
                $"Idempotency key must be between " +
                $"{IdempotencyKeyMinLength} and " +
                $"{IdempotencyKeyMaxLength} valid characters.",
                nameof(idempotencyKey));
        }

        return idempotencyKey.Trim();
    }

    private static string NormalizeRequestFingerprint(
        string requestFingerprint)
    {
        if (string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw new ArgumentException(
                "A request fingerprint is required.",
                nameof(requestFingerprint));
        }

        var normalizedFingerprint = requestFingerprint
            .Trim()
            .ToUpperInvariant();

        if (normalizedFingerprint.Length != RequestFingerprintLength ||
            normalizedFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                $"Request fingerprint must be a " +
                $"{RequestFingerprintLength}-character hexadecimal value.",
                nameof(requestFingerprint));
        }

        return normalizedFingerprint;
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "A currency is required.",
                nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != CurrencyLength ||
            normalizedCurrency.Any(character => !char.IsLetter(character)))
        {
            throw new ArgumentException(
                $"Currency must be a {CurrencyLength}-letter code.",
                nameof(currency));
        }

        return normalizedCurrency;
    }

    private static string NormalizeRequired(
        string value,
        int maximumLength,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} is required.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(
            value,
            maximumLength,
            displayName,
            parameterName);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
