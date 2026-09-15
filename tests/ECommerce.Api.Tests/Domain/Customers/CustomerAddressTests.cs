using ECommerce.Domain.Customers;

namespace ECommerce.Api.Tests.Domain.Customers;

public sealed class CustomerAddressTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 15, 15, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAtUtc =
        CreatedAtUtc.AddMinutes(10);

    [Fact]
    public void Constructor_WhenValuesAreValid_NormalizesAddress()
    {
        var customerId = Guid.NewGuid();

        var address = CreateAddress(
            customerId,
            label: "  Home  ",
            addressLine2: "   ",
            countryCode: "tr");

        Assert.Equal(customerId, address.CustomerId);
        Assert.Equal("Home", address.Label);
        Assert.Equal("Taylor Customer", address.RecipientFullName);
        Assert.Null(address.AddressLine2);
        Assert.Equal("TR", address.CountryCode);
        Assert.True(address.IsDefault);
        Assert.Equal(CreatedAtUtc, address.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, address.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WhenCustomerIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateAddress(Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenLabelIsBlank_Throws(string label)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateAddress(Guid.NewGuid(), label: label));
    }

    [Theory]
    [InlineData("T")]
    [InlineData("T1R")]
    [InlineData("1R")]
    public void Constructor_WhenCountryCodeIsInvalid_Throws(
        string countryCode)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateAddress(Guid.NewGuid(), countryCode: countryCode));
    }

    [Fact]
    public void Constructor_WhenOptionalAddressLineExceedsMaximum_Throws()
    {
        var addressLine2 = new string(
            'A',
            CustomerAddress.AddressLine2MaxLength + 1);

        Assert.Throws<ArgumentException>(() =>
            CreateAddress(
                Guid.NewGuid(),
                addressLine2: addressLine2));
    }

    [Fact]
    public void UpdateDetails_WhenValuesAreValid_ChangesMutableDetails()
    {
        var address = CreateAddress(Guid.NewGuid());

        address.UpdateDetails(
            "  Office  ",
            "  Taylor Buyer  ",
            "+90 555 444 55 66",
            "Business Avenue No: 20",
            "  Floor 2  ",
            "Besiktas",
            "Istanbul",
            "34340",
            "tr",
            isDefault: false,
            UpdatedAtUtc);

        Assert.Equal("Office", address.Label);
        Assert.Equal("Taylor Buyer", address.RecipientFullName);
        Assert.Equal("Floor 2", address.AddressLine2);
        Assert.Equal("TR", address.CountryCode);
        Assert.False(address.IsDefault);
        Assert.Equal(CreatedAtUtc, address.CreatedAtUtc);
        Assert.Equal(UpdatedAtUtc, address.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateDetails_WhenAnyValueIsInvalid_DoesNotChangeAddress()
    {
        var address = CreateAddress(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => address.UpdateDetails(
            "Office",
            "Taylor Buyer",
            "+90 555 444 55 66",
            "Business Avenue No: 20",
            addressLine2: null,
            "Besiktas",
            city: "   ",
            "34340",
            "TR",
            isDefault: false,
            UpdatedAtUtc));

        Assert.Equal("Home", address.Label);
        Assert.Equal("Taylor Customer", address.RecipientFullName);
        Assert.Equal("Kadikoy", address.District);
        Assert.Equal("Istanbul", address.City);
        Assert.True(address.IsDefault);
        Assert.Equal(CreatedAtUtc, address.UpdatedAtUtc);
    }

    private static CustomerAddress CreateAddress(
        Guid customerId,
        string label = "Home",
        string? addressLine2 = null,
        string countryCode = "TR")
    {
        return new CustomerAddress(
            customerId,
            label,
            "Taylor Customer",
            "+90 555 111 22 33",
            "Example Street No: 10",
            addressLine2,
            "Kadikoy",
            "Istanbul",
            "34710",
            countryCode,
            isDefault: true,
            CreatedAtUtc);
    }
}
