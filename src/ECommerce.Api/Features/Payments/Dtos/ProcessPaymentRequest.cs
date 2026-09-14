namespace ECommerce.Api.Features.Payments.Dtos;

public sealed class ProcessPaymentRequest
{
    public string PaymentMethodToken { get; set; } = string.Empty;
}
