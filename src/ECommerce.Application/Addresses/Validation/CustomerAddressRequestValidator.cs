using ECommerce.Application.Addresses.Dtos;
using ECommerce.Domain.Customers;

namespace ECommerce.Application.Addresses.Validation;

public static class CustomerAddressRequestValidator
{
    public const int MaximumAddressesPerCustomer =
        CustomerAddress.MaximumAddressesPerCustomer;

    public static Dictionary<string, string[]> Validate(
        CreateCustomerAddressRequest request)
    {
        return ValidateFields(
            request.Label,
            request.RecipientFullName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.District,
            request.City,
            request.PostalCode,
            request.CountryCode);
    }

    public static Dictionary<string, string[]> Validate(
        UpdateCustomerAddressRequest request)
    {
        return ValidateFields(
            request.Label,
            request.RecipientFullName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.District,
            request.City,
            request.PostalCode,
            request.CountryCode);
    }

    private static Dictionary<string, string[]> ValidateFields(
        string label,
        string recipientFullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string district,
        string city,
        string postalCode,
        string countryCode)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequiredLengthError(
            errors,
            "label",
            label,
            CustomerAddress.LabelMinLength,
            CustomerAddress.LabelMaxLength,
            "Label");

        AddRequiredLengthError(
            errors,
            "recipientFullName",
            recipientFullName,
            CustomerAddress.RecipientFullNameMinLength,
            CustomerAddress.RecipientFullNameMaxLength,
            "Recipient full name");

        AddRequiredLengthError(
            errors,
            "phoneNumber",
            phoneNumber,
            CustomerAddress.PhoneNumberMinLength,
            CustomerAddress.PhoneNumberMaxLength,
            "Phone number");

        AddRequiredLengthError(
            errors,
            "addressLine1",
            addressLine1,
            CustomerAddress.AddressLine1MinLength,
            CustomerAddress.AddressLine1MaxLength,
            "Address line 1");

        if (!string.IsNullOrWhiteSpace(addressLine2) &&
            addressLine2.Trim().Length > CustomerAddress.AddressLine2MaxLength)
        {
            errors["addressLine2"] = new[]
            {
                $"Address line 2 cannot exceed " +
                $"{CustomerAddress.AddressLine2MaxLength} characters."
            };
        }

        AddRequiredLengthError(
            errors,
            "district",
            district,
            CustomerAddress.DistrictMinLength,
            CustomerAddress.DistrictMaxLength,
            "District");

        AddRequiredLengthError(
            errors,
            "city",
            city,
            CustomerAddress.CityMinLength,
            CustomerAddress.CityMaxLength,
            "City");

        AddRequiredLengthError(
            errors,
            "postalCode",
            postalCode,
            CustomerAddress.PostalCodeMinLength,
            CustomerAddress.PostalCodeMaxLength,
            "Postal code");

        var normalizedCountryCode = countryCode?.Trim();

        if (normalizedCountryCode is null ||
            normalizedCountryCode.Length != CustomerAddress.CountryCodeLength ||
            normalizedCountryCode.Any(character => !char.IsLetter(character)))
        {
            errors["countryCode"] = new[]
            {
                "Country code must contain exactly two letters."
            };
        }

        return errors;
    }

    private static void AddRequiredLengthError(
        Dictionary<string, string[]> errors,
        string key,
        string value,
        int minimumLength,
        int maximumLength,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = new[]
            {
                $"{displayName} is required."
            };

            return;
        }

        var length = value.Trim().Length;

        if (length < minimumLength || length > maximumLength)
        {
            errors[key] = new[]
            {
                $"{displayName} must be between {minimumLength} and {maximumLength} characters."
            };
        }
    }
}
