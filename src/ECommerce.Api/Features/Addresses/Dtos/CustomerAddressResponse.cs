namespace ECommerce.Api.Features.Addresses.Dtos;

public sealed record CustomerAddressResponse(
    int Id,
    string Label,
    string RecipientFullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string District,
    string City,
    string PostalCode,
    string CountryCode,
    bool IsDefault,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
