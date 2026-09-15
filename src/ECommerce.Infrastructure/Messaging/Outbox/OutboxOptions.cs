namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaximumAttempts { get; set; } = 5;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(1);
}
