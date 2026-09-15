using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Outcomes;
using ECommerce.Domain.Customers;

namespace ECommerce.Application.Addresses.Services;

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
