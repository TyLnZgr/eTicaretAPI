using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Addresses.Outcomes;

public sealed record CustomerAddressMutationResult(
    CustomerAddressMutationStatus Status,
    CustomerAddress? Address = null);
