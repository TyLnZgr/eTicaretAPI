using ECommerce.Api.Identity;

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
}
