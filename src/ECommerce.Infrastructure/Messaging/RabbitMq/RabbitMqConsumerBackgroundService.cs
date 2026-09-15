using System.Text.Json;
using ECommerce.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConsumerBackgroundService : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly RabbitMqOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RabbitMqConsumerBackgroundService> _logger;

    public RabbitMqConsumerBackgroundService(
        IRabbitMqConnection connection,
        IIntegrationEventDispatcher dispatcher,
        IOptions<MessagingOptions> options,
        TimeProvider timeProvider,
        ILogger<RabbitMqConsumerBackgroundService> logger)
    {
        _connection = connection;
        _dispatcher = dispatcher;
        _options = options.Value.RabbitMq;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "RabbitMQ consumer stopped unexpectedly and will reconnect.");

                await Task.Delay(
                    _options.InitialConnectionRetryDelay,
                    _timeProvider,
                    stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var connection = await _connection.GetConnectionAsync(stoppingToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(
            channel,
            _options,
            stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var channelClosed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        channel.ChannelShutdownAsync += (_, _) =>
        {
            channelClosed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();

            try
            {
                var message = JsonSerializer.Deserialize<OutboxEnvelope>(
                    body,
                    JsonSerializerOptions.Web)
                    ?? throw new JsonException(
                        "The RabbitMQ event envelope is empty.");

                await _dispatcher.DispatchAsync(
                    message,
                    eventArgs.CancellationToken);

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    eventArgs.CancellationToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                await TryNackAsync(
                    channel,
                    eventArgs.DeliveryTag,
                    requeue: true);
            }
            catch (Exception exception)
            {
                var requeue = !eventArgs.Redelivered;

                _logger.LogError(
                    exception,
                    "RabbitMQ message {MessageId} failed. Requeue: {Requeue}.",
                    eventArgs.BasicProperties.MessageId,
                    requeue);

                await TryNackAsync(
                    channel,
                    eventArgs.DeliveryTag,
                    requeue);
            }
        };

        await channel.BasicConsumeAsync(
            _options.Queue,
            autoAck: false,
            consumer,
            stoppingToken);

        _logger.LogInformation(
            "RabbitMQ consumer is listening on queue {QueueName}.",
            _options.Queue);

        await channelClosed.Task.WaitAsync(stoppingToken);
    }

    private static async Task TryNackAsync(
        IChannel channel,
        ulong deliveryTag,
        bool requeue)
    {
        try
        {
            await channel.BasicNackAsync(
                deliveryTag,
                multiple: false,
                requeue,
                CancellationToken.None);
        }
        catch
        {
            // Closing the channel requeues every unacknowledged delivery.
        }
    }
}
