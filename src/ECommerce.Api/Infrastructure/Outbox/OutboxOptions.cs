namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
}
