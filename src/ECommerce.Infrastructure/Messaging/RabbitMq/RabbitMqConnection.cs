using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnection
    : IRabbitMqConnection,
        IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<MessagingOptions> options)
    {
        _options = options.Value.RabbitMq;
    }

    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                ClientProvidedName =
                    $"ecommerce-api:{Environment.MachineName}:{Environment.ProcessId}"
            };

            _connection = await factory.CreateConnectionAsync(
                cancellationToken);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
