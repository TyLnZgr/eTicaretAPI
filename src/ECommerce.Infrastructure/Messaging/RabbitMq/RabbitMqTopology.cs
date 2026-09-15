using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Messaging.RabbitMq;

public static class RabbitMqTopology
{
    public static async Task DeclareAsync(
        IChannel channel,
        RabbitMqOptions options,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            options.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            options.DeadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchange
        };

        await channel.QueueDeclareAsync(
            options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            options.Queue,
            options.Exchange,
            routingKey: "#",
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            options.DeadLetterQueue,
            options.DeadLetterExchange,
            routingKey: "#",
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
