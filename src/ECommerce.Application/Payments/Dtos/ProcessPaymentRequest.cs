namespace ECommerce.Application.Payments.Dtos;

public sealed class ProcessPaymentRequest
{
    public string PaymentMethodToken { get; set; } = string.Empty;
}
