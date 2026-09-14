using RabbitMQ.Client;

namespace ECommerce.Api.Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqConnection
{
    Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default);
}
