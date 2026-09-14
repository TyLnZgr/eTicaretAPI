namespace ECommerce.Api.Features.Orders.Dtos;

public sealed record OrderAddressResponse(
    string RecipientFullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string District,
    string City,
    string PostalCode,
    string CountryCode);
