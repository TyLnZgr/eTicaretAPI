using ECommerce.Api.Identity;
using ECommerce.Domain.Customers;

namespace ECommerce.Api.Tests.Infrastructure;

internal static class TestEntityFactory
{
    public static ApplicationUser CreateUser(
        Guid id,
        string email)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }

    public static CustomerAddress CreateAddress(
        Guid customerId,
        string recipientFullName = "Taylor Customer",
        string addressLine1 = "Example Street No: 10",
        bool isDefault = true)
    {
        return new CustomerAddress
        {
            CustomerId = customerId,
            Label = "Home",
            RecipientFullName = recipientFullName,
            PhoneNumber = "+90 555 111 22 33",
            AddressLine1 = addressLine1,
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            CountryCode = "TR",
            IsDefault = isDefault,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
