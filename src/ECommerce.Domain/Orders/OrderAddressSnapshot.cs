namespace ECommerce.Domain.Orders;

public sealed class OrderAddressSnapshot
{
    public const int RecipientFullNameMinLength = 2;
    public const int RecipientFullNameMaxLength = 200;
    public const int PhoneNumberMinLength = 3;
    public const int PhoneNumberMaxLength = 30;
    public const int AddressLine1MinLength = 5;
    public const int AddressLine1MaxLength = 300;
    public const int AddressLine2MaxLength = 300;
    public const int DistrictMinLength = 1;
    public const int DistrictMaxLength = 100;
    public const int CityMinLength = 1;
    public const int CityMaxLength = 100;
    public const int PostalCodeMinLength = 1;
    public const int PostalCodeMaxLength = 20;
    public const int CountryCodeLength = 2;

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
        RecipientFullName = NormalizeRequired(
            recipientFullName,
            RecipientFullNameMinLength,
            RecipientFullNameMaxLength,
            "Recipient full name",
            nameof(recipientFullName));
        PhoneNumber = NormalizeRequired(
            phoneNumber,
            PhoneNumberMinLength,
            PhoneNumberMaxLength,
            "Phone number",
            nameof(phoneNumber));
        AddressLine1 = NormalizeRequired(
            addressLine1,
            AddressLine1MinLength,
            AddressLine1MaxLength,
            "Address line 1",
            nameof(addressLine1));
        AddressLine2 = NormalizeOptional(
            addressLine2,
            AddressLine2MaxLength,
            "Address line 2",
            nameof(addressLine2));
        District = NormalizeRequired(
            district,
            DistrictMinLength,
            DistrictMaxLength,
            "District",
            nameof(district));
        City = NormalizeRequired(
            city,
            CityMinLength,
            CityMaxLength,
            "City",
            nameof(city));
        PostalCode = NormalizeRequired(
            postalCode,
            PostalCodeMinLength,
            PostalCodeMaxLength,
            "Postal code",
            nameof(postalCode));
        CountryCode = NormalizeCountryCode(countryCode);
    }

    public string RecipientFullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;

    private static string NormalizeRequired(
        string value,
        int minimumLength,
        int maximumLength,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} is required.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length < minimumLength ||
            normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} must be between {minimumLength} and " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException(
                "Country code is required.",
                nameof(countryCode));
        }

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();

        if (normalizedCountryCode.Length != CountryCodeLength ||
            normalizedCountryCode.Any(character => !char.IsLetter(character)))
        {
            throw new ArgumentException(
                $"Country code must contain exactly {CountryCodeLength} letters.",
                nameof(countryCode));
        }

        return normalizedCountryCode;
    }
}
