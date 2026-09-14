namespace ECommerce.Api.Infrastructure.Outbox;

public sealed record OutboxProcessingResult(
    int SelectedCount,
    int ProcessedCount,
    int FailedCount,
    int DeadLetteredCount,
    int LeaseLostCount);
