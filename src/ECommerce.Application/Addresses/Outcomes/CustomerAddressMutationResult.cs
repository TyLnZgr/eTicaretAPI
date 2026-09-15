using ECommerce.Domain.Customers;

namespace ECommerce.Application.Addresses.Outcomes;

public sealed record CustomerAddressMutationResult(
    CustomerAddressMutationStatus Status,
    CustomerAddress? Address = null);
