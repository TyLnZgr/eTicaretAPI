namespace ECommerce.Infrastructure.Operations.Outbox.Outcomes;

public enum OutboxRetryStatus
{
    Success,
    MessageNotFound,
    MessageNotDeadLettered
}
