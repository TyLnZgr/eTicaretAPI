using ECommerce.Application.Addresses.Dtos;
using ECommerce.Domain.Customers;

namespace ECommerce.Application.Addresses.Mappings;

public static class CustomerAddressMappings
{
    public static CustomerAddressResponse ToResponse(
        this CustomerAddress address)
    {
        return new CustomerAddressResponse(
            address.Id,
            address.Label,
            address.RecipientFullName,
            address.PhoneNumber,
            address.AddressLine1,
            address.AddressLine2,
            address.District,
            address.City,
            address.PostalCode,
            address.CountryCode,
            address.IsDefault,
            address.CreatedAtUtc,
            address.UpdatedAtUtc);
    }
}
