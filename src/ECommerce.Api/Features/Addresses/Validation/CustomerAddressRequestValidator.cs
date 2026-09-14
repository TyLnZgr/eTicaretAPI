using ECommerce.Api.Features.Addresses.Dtos;

namespace ECommerce.Api.Features.Addresses.Validation;

public static class CustomerAddressRequestValidator
{
    public const int MaximumAddressesPerCustomer = 20;

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
            minimumLength: 1,
            maximumLength: 100,
            "Label");

        AddRequiredLengthError(
            errors,
            "recipientFullName",
            recipientFullName,
            minimumLength: 2,
            maximumLength: 200,
            "Recipient full name");

        AddRequiredLengthError(
            errors,
            "phoneNumber",
            phoneNumber,
            minimumLength: 3,
            maximumLength: 30,
            "Phone number");

        AddRequiredLengthError(
            errors,
            "addressLine1",
            addressLine1,
            minimumLength: 5,
            maximumLength: 300,
            "Address line 1");

        if (!string.IsNullOrWhiteSpace(addressLine2) &&
            addressLine2.Trim().Length > 300)
        {
            errors["addressLine2"] = new[]
            {
                "Address line 2 cannot exceed 300 characters."
            };
        }

        AddRequiredLengthError(
            errors,
            "district",
            district,
            minimumLength: 1,
            maximumLength: 100,
            "District");

        AddRequiredLengthError(
            errors,
            "city",
            city,
            minimumLength: 1,
            maximumLength: 100,
            "City");

        AddRequiredLengthError(
            errors,
            "postalCode",
            postalCode,
            minimumLength: 1,
            maximumLength: 20,
            "Postal code");

        var normalizedCountryCode = countryCode?.Trim();

        if (normalizedCountryCode is null ||
            normalizedCountryCode.Length != 2 ||
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
