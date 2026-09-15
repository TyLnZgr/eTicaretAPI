using ECommerce.Infrastructure.Operations.Outbox.Dtos;
using ECommerce.Infrastructure.Operations.Outbox.Outcomes;
using ECommerce.Infrastructure.Messaging.Outbox;

namespace ECommerce.Infrastructure.Operations.Outbox.Mappings;

public static class OutboxAdministrationMappings
{
    public static OutboxMessageResponse ToAdministrationResponse(
        this OutboxMessage message,
        DateTime now)
    {
        return new OutboxMessageResponse(
            message.Id,
            message.Type,
            message.DeduplicationKey,
            GetState(message, now).ToString(),
            message.OccurredAtUtc,
            message.ProcessedAtUtc,
            message.AttemptCount,
            message.NextAttemptAtUtc,
            message.LastError,
            message.LockedBy,
            message.LockedUntilUtc,
            message.DeadLetteredAtUtc);
    }

    public static OutboxMessageState GetState(
        OutboxMessage message,
        DateTime now)
    {
        if (message.ProcessedAtUtc.HasValue)
        {
            return OutboxMessageState.Processed;
        }

        if (message.DeadLetteredAtUtc.HasValue)
        {
            return OutboxMessageState.DeadLettered;
        }

        if (message.LockId.HasValue && message.LockedUntilUtc > now)
        {
            return OutboxMessageState.Processing;
        }

        return message.AttemptCount > 0
            ? OutboxMessageState.Retrying
            : OutboxMessageState.Pending;
    }
}
