namespace ECommerce.Domain.Customers;

public sealed class CustomerAddress
{
    public const int MaximumAddressesPerCustomer = 20;
    public const int LabelMinLength = 1;
    public const int LabelMaxLength = 100;
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

    internal CustomerAddress()
    {
    }

    public CustomerAddress(
        Guid customerId,
        string label,
        string recipientFullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string district,
        string city,
        string postalCode,
        string countryCode,
        bool isDefault,
        DateTime createdAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "A customer ID is required.",
                nameof(customerId));
        }

        CustomerId = customerId;
        CreatedAtUtc = NormalizeUtc(createdAtUtc);

        UpdateDetails(
            label,
            recipientFullName,
            phoneNumber,
            addressLine1,
            addressLine2,
            district,
            city,
            postalCode,
            countryCode,
            isDefault,
            createdAtUtc);
    }

    public int Id { get; set; }
    public Guid CustomerId { get; internal set; }
    public string Label { get; internal set; } = string.Empty;
    public string RecipientFullName { get; internal set; } = string.Empty;
    public string PhoneNumber { get; internal set; } = string.Empty;
    public string AddressLine1 { get; internal set; } = string.Empty;
    public string? AddressLine2 { get; internal set; }
    public string District { get; internal set; } = string.Empty;
    public string City { get; internal set; } = string.Empty;
    public string PostalCode { get; internal set; } = string.Empty;
    public string CountryCode { get; internal set; } = string.Empty;
    public bool IsDefault { get; internal set; }
    public DateTime CreatedAtUtc { get; internal set; }
    public DateTime UpdatedAtUtc { get; internal set; }

    public void UpdateDetails(
        string label,
        string recipientFullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string district,
        string city,
        string postalCode,
        string countryCode,
        bool isDefault,
        DateTime updatedAtUtc)
    {
        var normalizedLabel = NormalizeRequired(
            label,
            LabelMinLength,
            LabelMaxLength,
            "Address label",
            nameof(label));
        var normalizedRecipientFullName = NormalizeRequired(
            recipientFullName,
            RecipientFullNameMinLength,
            RecipientFullNameMaxLength,
            "Recipient full name",
            nameof(recipientFullName));
        var normalizedPhoneNumber = NormalizeRequired(
            phoneNumber,
            PhoneNumberMinLength,
            PhoneNumberMaxLength,
            "Phone number",
            nameof(phoneNumber));
        var normalizedAddressLine1 = NormalizeRequired(
            addressLine1,
            AddressLine1MinLength,
            AddressLine1MaxLength,
            "Address line 1",
            nameof(addressLine1));
        var normalizedAddressLine2 = NormalizeOptional(
            addressLine2,
            AddressLine2MaxLength,
            "Address line 2",
            nameof(addressLine2));
        var normalizedDistrict = NormalizeRequired(
            district,
            DistrictMinLength,
            DistrictMaxLength,
            "District",
            nameof(district));
        var normalizedCity = NormalizeRequired(
            city,
            CityMinLength,
            CityMaxLength,
            "City",
            nameof(city));
        var normalizedPostalCode = NormalizeRequired(
            postalCode,
            PostalCodeMinLength,
            PostalCodeMaxLength,
            "Postal code",
            nameof(postalCode));
        var normalizedCountryCode = NormalizeCountryCode(countryCode);

        Label = normalizedLabel;
        RecipientFullName = normalizedRecipientFullName;
        PhoneNumber = normalizedPhoneNumber;
        AddressLine1 = normalizedAddressLine1;
        AddressLine2 = normalizedAddressLine2;
        District = normalizedDistrict;
        City = normalizedCity;
        PostalCode = normalizedPostalCode;
        CountryCode = normalizedCountryCode;
        IsDefault = isDefault;
        UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
    }

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

    private static DateTime NormalizeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
