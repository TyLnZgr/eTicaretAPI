using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ECommerce.Api.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Tests.Infrastructure;

internal sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuthentication";

    private const string UserIdHeaderName = "X-Test-User-Id";
    private const string EmailHeaderName = "X-Test-User-Email";
    private const string RoleHeaderName = "X-Test-User-Role";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                UserIdHeaderName,
                out var userIdValue) ||
            !Guid.TryParse(userIdValue, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = Request.Headers.TryGetValue(
            EmailHeaderName,
            out var emailValue)
                ? emailValue.ToString()
                : "customer@example.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email)
        };

        if (Request.Headers.TryGetValue(
                RoleHeaderName,
                out var roleValue) &&
            !string.IsNullOrWhiteSpace(roleValue))
        {
            claims.Add(new Claim(
                ClaimTypes.Role,
                roleValue.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }

    public static void Authenticate(
        HttpClient client,
        Guid userId,
        string email,
        bool isAdministrator)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(SchemeName);

        client.DefaultRequestHeaders.Add(
            UserIdHeaderName,
            userId.ToString());

        client.DefaultRequestHeaders.Add(
            EmailHeaderName,
            email);

        if (isAdministrator)
        {
            client.DefaultRequestHeaders.Add(
                RoleHeaderName,
                AppRoles.Administrator);
        }
    }
}
