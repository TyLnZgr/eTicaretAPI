using System.Net;
using System.Security.Claims;

namespace ECommerce.Api.Common.RateLimiting;

internal static class RateLimitPartitionKey
{
    public static string Create(HttpContext context, string policyName)
    {
        var userId = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (context.User.Identity?.IsAuthenticated is true &&
            !string.IsNullOrWhiteSpace(userId))
        {
            return $"{policyName}:user:{userId}";
        }

        var remoteIpAddress = Normalize(
            context.Connection.RemoteIpAddress);

        return $"{policyName}:ip:{remoteIpAddress}";
    }

    private static string Normalize(IPAddress? remoteIpAddress)
    {
        if (remoteIpAddress is null)
        {
            return "unknown";
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            return remoteIpAddress.MapToIPv4().ToString();
        }

        return remoteIpAddress.ToString();
    }
}
