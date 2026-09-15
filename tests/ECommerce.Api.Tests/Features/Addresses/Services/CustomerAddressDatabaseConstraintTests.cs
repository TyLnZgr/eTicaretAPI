using ECommerce.Api.Tests.Infrastructure;
using ECommerce.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Addresses.Services;

public sealed class CustomerAddressDatabaseConstraintTests
{
    [Fact]
    public async Task SaveChangesAsync_WithTwoDefaultAddressesForOneCustomer_IsRejected()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var customerId = Guid.NewGuid();
        database.DbContext.Users.Add(TestEntityFactory.CreateUser(
            customerId,
            "customer@example.com"));
        database.DbContext.CustomerAddresses.AddRange(
            CreateAddress(customerId, "Home"),
            CreateAddress(customerId, "Office"));

        // Act
        var saveAction = () =>
            database.DbContext.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(saveAction);
    }

    private static CustomerAddress CreateAddress(
        Guid customerId,
        string label)
    {
        return new CustomerAddress(
            customerId,
            label,
            "Taylor Customer",
            "+90 555 111 22 33",
            "Example Street No: 10",
            addressLine2: null,
            "Kadikoy",
            "Istanbul",
            "34710",
            "TR",
            isDefault: true,
            DateTime.UtcNow);
    }
}
