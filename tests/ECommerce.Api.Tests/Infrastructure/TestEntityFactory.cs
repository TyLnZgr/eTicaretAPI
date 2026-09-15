using ECommerce.Infrastructure.Identity;
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
        return new CustomerAddress(
            customerId,
            "Home",
            recipientFullName,
            "+90 555 111 22 33",
            addressLine1,
            addressLine2: null,
            "Kadikoy",
            "Istanbul",
            "34710",
            "TR",
            isDefault,
            DateTime.UtcNow);
    }
}
