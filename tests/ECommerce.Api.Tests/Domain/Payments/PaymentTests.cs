using ECommerce.Domain.Payments;

namespace ECommerce.Api.Tests.Domain.Payments;

public sealed class PaymentTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 15, 16, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc =
        CreatedAtUtc.AddMinutes(1);

    [Fact]
    public void Constructor_WhenValuesAreValid_NormalizesPayment()
    {
        var payment = new Payment(
            orderId: 1,
            idempotencyKey: "  payment-test-001  ",
            requestFingerprint: new string('a', 64),
            amount: 1000m,
            currency: "try",
            provider: "  FakePay  ",
            CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(1, payment.OrderId);
        Assert.Equal("payment-test-001", payment.IdempotencyKey);
        Assert.Equal(new string('A', 64), payment.RequestFingerprint);
        Assert.Equal(1000m, payment.Amount);
        Assert.Equal("TRY", payment.Currency);
        Assert.Equal("FakePay", payment.Provider);
        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal(CreatedAtUtc, payment.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, payment.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(-1, 1000)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_WhenNumericValuesAreInvalid_Throws(
        int orderId,
        decimal amount)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new Payment(
            orderId,
            "payment-test-001",
            new string('A', 64),
            amount,
            "TRY",
            "FakePay",
            CreatedAtUtc));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("invalid key")]
    public void Constructor_WhenIdempotencyKeyIsInvalid_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => new Payment(
            orderId: 1,
            idempotencyKey: key,
            requestFingerprint: new string('A', 64),
            amount: 1000m,
            currency: "TRY",
            provider: "FakePay",
            CreatedAtUtc));
    }

    [Fact]
    public void MarkSucceeded_WhenProcessing_NormalizesGatewayResult()
    {
        var payment = CreatePayment();

        payment.MarkSucceeded("  provider-payment-1  ", UpdatedAtUtc);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("provider-payment-1", payment.ProviderPaymentId);
        Assert.Null(payment.FailureCode);
        Assert.Equal(UpdatedAtUtc, payment.UpdatedAtUtc);
    }

    [Fact]
    public void MarkFailed_WhenProcessing_NormalizesGatewayResult()
    {
        var payment = CreatePayment();

        payment.MarkFailed(
            "  payment_method_declined  ",
            providerPaymentId: "   ",
            UpdatedAtUtc);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("payment_method_declined", payment.FailureCode);
        Assert.Null(payment.ProviderPaymentId);
        Assert.Equal(UpdatedAtUtc, payment.UpdatedAtUtc);
    }

    [Fact]
    public void MarkFailed_AfterPaymentSucceeded_RejectsSecondFinalization()
    {
        // Arrange
        var payment = CreatePayment();

        payment.MarkSucceeded(
            "provider-payment-1",
            UpdatedAtUtc);

        // Act
        var finalizeAgainAction = () => payment.MarkFailed(
            "payment_method_declined",
            providerPaymentId: null,
            UpdatedAtUtc.AddMinutes(1));

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(
            finalizeAgainAction);

        Assert.Equal(
            "Only a processing payment can be finalized.",
            exception.Message);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(
            "provider-payment-1",
            payment.ProviderPaymentId);
        Assert.Null(payment.FailureCode);
    }

    private static Payment CreatePayment()
    {
        return new Payment(
            orderId: 1,
            idempotencyKey: "payment-test-001",
            requestFingerprint: new string('A', 64),
            amount: 1000m,
            currency: "TRY",
            provider: "FakePay",
            CreatedAtUtc);
    }
}
