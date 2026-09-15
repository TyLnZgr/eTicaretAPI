using ECommerce.Application.Common.Pagination;
using ECommerce.Api.Features.Operations.Outbox.Dtos;
using ECommerce.Api.Features.Operations.Outbox.Outcomes;

namespace ECommerce.Api.Features.Operations.Outbox.Services;

public interface IOutboxAdministrationService
{
    Task<PagedResult<OutboxMessageResponse>> GetAllAsync(
        OutboxMessageQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<OutboxRetryStatus> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
