namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed record OutboxProcessingResult(
    int SelectedCount,
    int ProcessedCount,
    int FailedCount,
    int DeadLetteredCount,
    int LeaseLostCount);
