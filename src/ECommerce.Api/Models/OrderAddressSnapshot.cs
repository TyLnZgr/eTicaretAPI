namespace ECommerce.Api.Models;

public sealed class OrderAddressSnapshot
{
    private OrderAddressSnapshot()
    {
    }

    public OrderAddressSnapshot(
        string recipientFullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string district,
        string city,
        string postalCode,
        string countryCode)
    {
        RecipientFullName = recipientFullName;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        District = district;
        City = city;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public string RecipientFullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
}
