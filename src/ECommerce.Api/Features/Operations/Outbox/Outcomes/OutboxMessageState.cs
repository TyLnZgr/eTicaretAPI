namespace ECommerce.Api.Features.Operations.Outbox.Outcomes;

public enum OutboxMessageState
{
    Pending,
    Retrying,
    Processing,
    Processed,
    DeadLettered
}
