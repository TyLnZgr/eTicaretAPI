using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Features.Addresses.Dtos;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Addresses.Endpoints;

public sealed class CustomerAddressEndpointTests
{
    public static TheoryData<string, string> ProtectedAddressEndpoints =>
        new()
        {
            { "GET", "/api/addresses" },
            { "GET", "/api/addresses/1" },
            { "POST", "/api/addresses" },
            { "PUT", "/api/addresses/1" },
            { "DELETE", "/api/addresses/1" }
        };

    [Theory]
    [MemberData(nameof(ProtectedAddressEndpoints))]
    public async Task AddressEndpoint_WithoutAuthentication_ReturnsUnauthorized(
        string method,
        string requestUri)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(
                new HttpMethod(method),
                requestUri));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenFirstAddress_IgnoresFalseFlagAndMakesItDefault()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            await dbContext.SaveChangesAsync();
        });

        var request = CreateRequest();
        request.Label = "  Home  ";
        request.CountryCode = "tr";
        request.AddressLine2 = "   ";
        request.IsDefault = false;

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/addresses",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var address = await response.Content
            .ReadFromJsonAsync<CustomerAddressResponse>();

        Assert.NotNull(address);
        Assert.Equal("Home", address.Label);
        Assert.Equal("TR", address.CountryCode);
        Assert.Null(address.AddressLine2);
        Assert.True(address.IsDefault);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/addresses/{address.Id}",
            response.Headers.Location.AbsolutePath);
    }

    [Fact]
    public async Task CreateAsync_WhenNewAddressIsDefault_SwitchesPreviousDefault()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            await dbContext.SaveChangesAsync();
        });

        var homeRequest = CreateRequest();
        homeRequest.Label = "Home";

        using var homeResponse = await client.PostAsJsonAsync(
            "/api/addresses",
            homeRequest);

        var officeRequest = CreateRequest();
        officeRequest.Label = "Office";
        officeRequest.IsDefault = true;

        // Act
        using var officeResponse = await client.PostAsJsonAsync(
            "/api/addresses",
            officeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, homeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, officeResponse.StatusCode);

        var addresses = await client
            .GetFromJsonAsync<CustomerAddressResponse[]>(
                "/api/addresses");

        Assert.NotNull(addresses);
        Assert.Equal(2, addresses.Length);
        Assert.Equal("Office", addresses[0].Label);
        Assert.True(addresses[0].IsDefault);
        Assert.Equal("Home", addresses[1].Label);
        Assert.False(addresses[1].IsDefault);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(
                1,
                await dbContext.CustomerAddresses.CountAsync(
                    address => address.IsDefault));
        });
    }

    [Fact]
    public async Task GetUpdateDeleteAsync_AsDifferentCustomer_ReturnNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        using var intruderClient = factory.CreateCustomerClient(intruderId);
        var addressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                ownerId,
                "owner@example.com"));

            var address = CreateAddress(ownerId, "Home", isDefault: true);
            dbContext.CustomerAddresses.Add(address);
            await dbContext.SaveChangesAsync();

            addressId = address.Id;
        });

        var updateRequest = new UpdateCustomerAddressRequest
        {
            Label = "Changed",
            RecipientFullName = "Different Customer",
            PhoneNumber = "+90 555 111 22 33",
            AddressLine1 = "Unknown Street 10",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            CountryCode = "TR",
            IsDefault = true
        };

        // Act
        using var getResponse = await intruderClient.GetAsync(
            $"/api/addresses/{addressId}");
        using var updateResponse = await intruderClient.PutAsJsonAsync(
            $"/api/addresses/{addressId}",
            updateRequest);
        using var deleteResponse = await intruderClient.DeleteAsync(
            $"/api/addresses/{addressId}");

        // Assert
        await AssertAddressNotFoundAsync(getResponse, addressId);
        await AssertAddressNotFoundAsync(updateResponse, addressId);
        await AssertAddressNotFoundAsync(deleteResponse, addressId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var address = await dbContext.CustomerAddresses
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal("Home", address.Label);
        });
    }

    [Fact]
    public async Task UpdateAsync_WhenAddressBecomesDefault_ClearsPreviousDefault()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var officeAddressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var homeAddress = CreateAddress(
                customerId,
                "Home",
                isDefault: true);
            var officeAddress = CreateAddress(
                customerId,
                "Office",
                isDefault: false);

            dbContext.CustomerAddresses.AddRange(
                homeAddress,
                officeAddress);
            await dbContext.SaveChangesAsync();

            officeAddressId = officeAddress.Id;
        });

        var request = new UpdateCustomerAddressRequest
        {
            Label = "  Main Office  ",
            RecipientFullName = "Taylor Customer",
            PhoneNumber = "+90 555 111 22 33",
            AddressLine1 = "Business Avenue No: 20",
            AddressLine2 = "   ",
            District = "Besiktas",
            City = "Istanbul",
            PostalCode = "34340",
            CountryCode = "tr",
            IsDefault = true
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/addresses/{officeAddressId}",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedAddress = await response.Content
            .ReadFromJsonAsync<CustomerAddressResponse>();

        Assert.NotNull(updatedAddress);
        Assert.Equal("Main Office", updatedAddress.Label);
        Assert.Equal("TR", updatedAddress.CountryCode);
        Assert.Null(updatedAddress.AddressLine2);
        Assert.True(updatedAddress.IsDefault);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var addresses = await dbContext.CustomerAddresses
                .AsNoTracking()
                .OrderBy(address => address.Id)
                .ToArrayAsync();

            Assert.Equal(2, addresses.Length);
            Assert.False(addresses[0].IsDefault);
            Assert.True(addresses[1].IsDefault);
        });
    }

    [Fact]
    public async Task DeleteAsync_WhenDefaultAddressIsDeleted_PromotesMostRecentAddress()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var defaultAddressId = 0;
        var recentAddressId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            var defaultAddress = CreateAddress(
                customerId,
                "Home",
                isDefault: true,
                updatedAtUtc: new DateTime(2026, 9, 1));
            var recentAddress = CreateAddress(
                customerId,
                "Office",
                isDefault: false,
                updatedAtUtc: new DateTime(2026, 9, 2));
            var oldAddress = CreateAddress(
                customerId,
                "Family",
                isDefault: false,
                updatedAtUtc: new DateTime(2026, 8, 1));

            dbContext.CustomerAddresses.AddRange(
                defaultAddress,
                recentAddress,
                oldAddress);
            await dbContext.SaveChangesAsync();

            defaultAddressId = defaultAddress.Id;
            recentAddressId = recentAddress.Id;
        });

        // Act
        using var response = await client.DeleteAsync(
            $"/api/addresses/{defaultAddressId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var addresses = await dbContext.CustomerAddresses
                .AsNoTracking()
                .OrderBy(address => address.Id)
                .ToArrayAsync();

            Assert.Equal(2, addresses.Length);

            var defaultAddress = Assert.Single(
                addresses,
                address => address.IsDefault);

            Assert.Equal(recentAddressId, defaultAddress.Id);
        });
    }

    [Fact]
    public async Task CreateAsync_WhenRequiredFieldsAreInvalid_ReturnsValidationProblem()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        var request = CreateRequest();
        request.Label = "   ";

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/addresses",
            request);

        // Assert
        await ProblemDetailsAssertions.AssertValidationAsync(
            response,
            "label",
            "Label is required.");
    }

    [Fact]
    public async Task CreateAsync_WhenAddressLimitIsReached_ReturnsConflict()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                "customer@example.com"));

            for (var index = 1; index <= 20; index++)
            {
                dbContext.CustomerAddresses.Add(CreateAddress(
                    customerId,
                    $"Address {index}",
                    isDefault: index == 1));
            }

            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/addresses",
            CreateRequest());

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "A customer can save at most 20 addresses.");
    }

    private static CreateCustomerAddressRequest CreateRequest()
    {
        return new CreateCustomerAddressRequest
        {
            Label = "Home",
            RecipientFullName = "Taylor Customer",
            PhoneNumber = "+90 555 111 22 33",
            AddressLine1 = "Example Street No: 10",
            AddressLine2 = "Floor 2",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            CountryCode = "TR"
        };
    }

    private static CustomerAddress CreateAddress(
        Guid customerId,
        string label,
        bool isDefault,
        DateTime? updatedAtUtc = null)
    {
        var timestamp = updatedAtUtc ?? DateTime.UtcNow;

        return new CustomerAddress
        {
            CustomerId = customerId,
            Label = label,
            RecipientFullName = "Taylor Customer",
            PhoneNumber = "+90 555 111 22 33",
            AddressLine1 = "Example Street No: 10",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            CountryCode = "TR",
            IsDefault = isDefault,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };
    }

    private static Task AssertAddressNotFoundAsync(
        HttpResponseMessage response,
        int addressId)
    {
        return ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Address with ID {addressId} was not found.");
    }
}
