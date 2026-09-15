namespace ECommerce.Application.Addresses.Dtos;

public sealed class CreateCustomerAddressRequest
{
    public string Label { get; set; } = string.Empty;
    public string RecipientFullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "TR";
    public bool IsDefault { get; set; }
}
