namespace ECommerce.Application.Orders.IntegrationEvents;

public sealed record OrderPaidIntegrationEvent(
    int OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTime PaidAtUtc);
