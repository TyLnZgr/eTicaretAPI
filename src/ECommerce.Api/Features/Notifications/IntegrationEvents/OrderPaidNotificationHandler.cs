using System.Text.Json;
using ECommerce.Application.Orders.IntegrationEvents;
using ECommerce.Api.Data;
using ECommerce.Api.Infrastructure.Outbox;
using ECommerce.Domain.Notifications;
using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Notifications.IntegrationEvents;

public sealed class OrderPaidNotificationHandler
    : IIntegrationEventHandler
{

    public const string ConsumerName = "notifications.order-paid.v1";

    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public OrderPaidNotificationHandler(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public string EventType => IntegrationEventTypes.OrderPaidV1;

    public async Task HandleAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default)
    {
        if (await WasAlreadyProcessedAsync(
                message.MessageId,
                cancellationToken))
        {
            return;
        }

        var orderPaidEvent = JsonSerializer
            .Deserialize<OrderPaidIntegrationEvent>(
                message.Payload,
                JsonSerializerOptions.Web)
            ?? throw new JsonException(
                "The OrderPaid event payload is empty.");

        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == orderPaidEvent.PaymentId &&
                candidate.OrderId == orderPaidEvent.OrderId &&
                candidate.Status == PaymentStatus.Succeeded &&
                candidate.Amount == orderPaidEvent.Amount &&
                candidate.Currency == orderPaidEvent.Currency)
            .Select(candidate => new
            {
                candidate.Order.CustomerId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payment?.CustomerId is not Guid customerId)
        {
            throw new InvalidOperationException(
                "The OrderPaid event does not match a succeeded customer payment.");
        }

        var processedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.CustomerNotifications.Add(
            new CustomerNotification(
                customerId,
                orderPaidEvent.OrderId,
                message.MessageId,
                NotificationType.OrderPaid,
                "Payment received",
                $"Payment for order #{orderPaidEvent.OrderId} was received.",
                processedAtUtc));

        _dbContext.InboxMessages.Add(
            new InboxMessage(
                message.MessageId,
                ConsumerName,
                processedAtUtc));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();

            if (await WasAlreadyProcessedAsync(
                    message.MessageId,
                    cancellationToken))
            {
                return;
            }

            throw;
        }
    }

    private Task<bool> WasAlreadyProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.InboxMessages
            .AsNoTracking()
            .AnyAsync(
                inboxMessage =>
                    inboxMessage.MessageId == messageId &&
                    inboxMessage.ConsumerName == ConsumerName,
                cancellationToken);
    }
}
