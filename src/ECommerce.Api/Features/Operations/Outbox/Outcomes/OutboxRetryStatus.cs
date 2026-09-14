namespace ECommerce.Api.Features.Operations.Outbox.Outcomes;

public enum OutboxRetryStatus
{
    Success,
    MessageNotFound,
    MessageNotDeadLettered
}
