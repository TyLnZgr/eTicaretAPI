namespace ECommerce.Infrastructure.Operations.Outbox.Outcomes;

public enum OutboxMessageState
{
    Pending,
    Retrying,
    Processing,
    Processed,
    DeadLettered
}
