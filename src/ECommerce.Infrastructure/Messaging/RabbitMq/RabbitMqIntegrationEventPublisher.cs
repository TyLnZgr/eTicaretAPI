using System.Text.Json;
using ECommerce.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly IRabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqIntegrationEventPublisher(
        IRabbitMqConnection connection,
        IOptions<MessagingOptions> options)
    {
        _connection = connection;
        _options = options.Value.RabbitMq;
    }

    public async Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connection.GetConnectionAsync(
            cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await RabbitMqTopology.DeclareAsync(
            channel,
            _options,
            cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId.ToString("D"),
            Type = message.Type,
            Timestamp = new AmqpTimestamp(
                new DateTimeOffset(message.OccurredAtUtc)
                    .ToUnixTimeSeconds())
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(
            message,
            JsonSerializerOptions.Web);

        await channel.BasicPublishAsync(
            _options.Exchange,
            routingKey: message.Type,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
