using ECommerce.Domain.Orders;

namespace ECommerce.Domain.Payments;

public sealed class Payment
{
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
        Id = Guid.NewGuid();
        OrderId = orderId;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        Amount = amount;
        Currency = currency;
        Provider = provider;
        Status = PaymentStatus.Processing;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
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

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new ArgumentException(
                "A provider payment ID is required.",
                nameof(providerPaymentId));
        }

        Status = PaymentStatus.Succeeded;
        ProviderPaymentId = providerPaymentId;
        FailureCode = null;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void MarkFailed(
        string failureCode,
        string? providerPaymentId,
        DateTime updatedAtUtc)
    {
        EnsureProcessing();

        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException(
                "A failure code is required.",
                nameof(failureCode));
        }

        Status = PaymentStatus.Failed;
        ProviderPaymentId = providerPaymentId;
        FailureCode = failureCode;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureProcessing()
    {
        if (Status != PaymentStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only a processing payment can be finalized.");
        }
    }
}
