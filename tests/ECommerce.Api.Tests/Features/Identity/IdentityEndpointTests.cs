using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Infrastructure.Identity;
using ECommerce.Api.Identity.Authorization;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Api.Tests.Features.Identity;

public sealed class IdentityEndpointTests
{
    private const string ValidPassword = "StrongPass1!";

    [Fact]
    public async Task RegisterLoginAndRefresh_WhenCredentialsAreValid_ReturnTokens()
    {
        // Arrange
        using var factory = new ECommerceApiFactory(
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        const string email = "identity-user@example.com";

        // Act - register
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = ValidPassword
            });

        // Assert - register
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Act - login
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = ValidPassword
            });

        // Assert - login
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginTokens = await ReadTokensAsync(loginResponse);

        Assert.False(string.IsNullOrWhiteSpace(loginTokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginTokens.RefreshToken));
        Assert.True(loginTokens.ExpiresIn > 0);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginTokens.AccessToken);

        using var infoResponse = await client.GetAsync(
            "/api/auth/manage/info");

        Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);

        using var infoDocument = JsonDocument.Parse(
            await infoResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            email,
            infoDocument.RootElement
                .GetProperty("email")
                .GetString());

        // Act - refresh
        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken = loginTokens.RefreshToken
            });

        // Assert - refresh
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshedTokens = await ReadTokensAsync(refreshResponse);

        Assert.False(string.IsNullOrWhiteSpace(
            refreshedTokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(
            refreshedTokens.RefreshToken));
    }

    [Fact]
    public async Task Register_WhenPasswordDoesNotMeetPolicy_DoesNotCreateUser()
    {
        // Arrange
        using var factory = new ECommerceApiFactory(
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "weak-password@example.com",
                password = "weak"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.False(await dbContext.Users.AnyAsync(user =>
                user.Email == "weak-password@example.com"));
        });
    }

    [Fact]
    public async Task AdministratorRole_AppearsOnlyInTokenCreatedAfterRoleAssignment()
    {
        // Arrange
        using var factory = new ECommerceApiFactory(
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        const string email = "role-admin@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = ValidPassword
            });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var tokenBeforeRole = await LoginAsync(
            client,
            email,
            ValidPassword);

        using (var scope = factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var createRoleResult = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Name = AppRoles.Administrator
                });

            Assert.True(createRoleResult.Succeeded);

            var user = await userManager.FindByEmailAsync(email);

            Assert.NotNull(user);

            var addToRoleResult = await userManager.AddToRoleAsync(
                user,
                AppRoles.Administrator);

            Assert.True(addToRoleResult.Succeeded);
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenBeforeRole.AccessToken);

        // Act - old token
        using var oldTokenResponse = await client.GetAsync(
            "/api/admin/orders");

        // Assert - old token
        Assert.Equal(
            HttpStatusCode.Forbidden,
            oldTokenResponse.StatusCode);

        // Act - login again
        var tokenAfterRole = await LoginAsync(
            client,
            email,
            ValidPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenAfterRole.AccessToken);

        using var newTokenResponse = await client.GetAsync(
            "/api/admin/orders");

        // Assert - new token
        Assert.Equal(HttpStatusCode.OK, newTokenResponse.StatusCode);
    }

    private static async Task<TokenResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ReadTokensAsync(response);
    }

    private static async Task<TokenResponse> ReadTokensAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = document.RootElement;

        return new TokenResponse(
            root.GetProperty("accessToken").GetString() ?? string.Empty,
            root.GetProperty("refreshToken").GetString() ?? string.Empty,
            root.GetProperty("expiresIn").GetInt64());
    }

    private sealed record TokenResponse(
        string AccessToken,
        string RefreshToken,
        long ExpiresIn);
}
