using ECommerce.Api.Features.Addresses.Dtos;
using ECommerce.Api.Features.Addresses.Outcomes;
using ECommerce.Domain.Customers;

namespace ECommerce.Api.Features.Addresses.Services;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddress>> GetAllAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetByIdAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressMutationResult> CreateAsync(
        Guid customerId,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressMutationResult> UpdateAsync(
        int id,
        Guid customerId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressMutationStatus> DeleteAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
