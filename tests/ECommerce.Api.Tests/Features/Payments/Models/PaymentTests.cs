using ECommerce.Domain.Payments;

namespace ECommerce.Api.Tests.Features.Payments.Models;

public sealed class PaymentTests
{
    [Fact]
    public void MarkFailed_AfterPaymentSucceeded_RejectsSecondFinalization()
    {
        // Arrange
        var payment = new Payment(
            orderId: 1,
            idempotencyKey: "payment-test-001",
            requestFingerprint: new string('A', 64),
            amount: 1000m,
            currency: "TRY",
            provider: "FakePay",
            createdAtUtc: DateTime.UtcNow);

        payment.MarkSucceeded(
            "provider-payment-1",
            DateTime.UtcNow);

        // Act
        var finalizeAgainAction = () => payment.MarkFailed(
            "payment_method_declined",
            providerPaymentId: null,
            DateTime.UtcNow);

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
}
