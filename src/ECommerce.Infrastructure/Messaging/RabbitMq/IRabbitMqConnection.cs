using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqConnection
{
    Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default);
}
