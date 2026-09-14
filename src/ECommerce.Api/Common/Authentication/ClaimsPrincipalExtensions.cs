using System.Security.Claims;

namespace ECommerce.Api.Common.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out Guid userId)
    {
        var value = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}
