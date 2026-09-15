using ECommerce.Domain.Orders;

namespace ECommerce.Api.Tests.Domain.Orders;

public sealed class OrderAddressSnapshotTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_NormalizesSnapshot()
    {
        var snapshot = CreateSnapshot(
            recipientFullName: "  Taylor Customer  ",
            addressLine2: "   ",
            countryCode: "tr");

        Assert.Equal("Taylor Customer", snapshot.RecipientFullName);
        Assert.Equal("+90 555 111 22 33", snapshot.PhoneNumber);
        Assert.Equal("Example Street No: 10", snapshot.AddressLine1);
        Assert.Null(snapshot.AddressLine2);
        Assert.Equal("Kadikoy", snapshot.District);
        Assert.Equal("Istanbul", snapshot.City);
        Assert.Equal("34710", snapshot.PostalCode);
        Assert.Equal("TR", snapshot.CountryCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenRecipientNameIsBlank_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(recipientFullName: name));
    }

    [Theory]
    [InlineData("T")]
    [InlineData("TUR")]
    [InlineData("1R")]
    public void Constructor_WhenCountryCodeIsInvalid_Throws(string countryCode)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(countryCode: countryCode));
    }

    [Fact]
    public void Constructor_WhenAddressLine2ExceedsMaximum_Throws()
    {
        var addressLine2 = new string(
            'A',
            OrderAddressSnapshot.AddressLine2MaxLength + 1);

        Assert.Throws<ArgumentException>(() =>
            CreateSnapshot(addressLine2: addressLine2));
    }

    private static OrderAddressSnapshot CreateSnapshot(
        string recipientFullName = "Taylor Customer",
        string? addressLine2 = null,
        string countryCode = "TR")
    {
        return new OrderAddressSnapshot(
            recipientFullName,
            "+90 555 111 22 33",
            "Example Street No: 10",
            addressLine2,
            "Kadikoy",
            "Istanbul",
            "34710",
            countryCode);
    }
}
