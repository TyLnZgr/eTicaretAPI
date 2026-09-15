namespace ECommerce.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string Provider { get; set; } = MessagingProviders.InProcess;
    public RabbitMqOptions RabbitMq { get; set; } = new();
}

public static class MessagingProviders
{
    public const string InProcess = "InProcess";
    public const string RabbitMq = "RabbitMq";

    public static bool IsSupported(string? provider)
    {
        return string.Equals(
                provider,
                InProcess,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                provider,
                RabbitMq,
                StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "ecommerce.events";
    public string Queue { get; set; } = "ecommerce.api";
    public string DeadLetterExchange { get; set; } =
        "ecommerce.events.dead-letter";
    public string DeadLetterQueue { get; set; } =
        "ecommerce.api.dead-letter";
    public ushort PrefetchCount { get; set; } = 10;
    public TimeSpan InitialConnectionRetryDelay { get; set; } =
        TimeSpan.FromSeconds(5);
}
