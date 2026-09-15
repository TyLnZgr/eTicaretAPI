using ECommerce.Application.Common.Pagination;
using ECommerce.Infrastructure.Operations.Outbox.Dtos;
using ECommerce.Infrastructure.Operations.Outbox.Outcomes;

namespace ECommerce.Infrastructure.Operations.Outbox.Services;

public interface IOutboxAdministrationService
{
    Task<PagedResult<OutboxMessageResponse>> GetAllAsync(
        OutboxMessageQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<OutboxRetryStatus> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
