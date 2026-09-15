using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace ECommerce.Api.Common.Http;

public static class EntityTagHeader
{
    public static string Format(long version)
    {
        return $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";
    }

    public static bool TryParse(
        StringValues values,
        out long version)
    {
        version = default;

        if (values.Count != 1)
        {
            return false;
        }

        var value = values[0]?.Trim();

        if (string.IsNullOrEmpty(value) ||
            value.Length < 3 ||
            value[0] != '"' ||
            value[^1] != '"')
        {
            return false;
        }

        return long.TryParse(
                   value.AsSpan(1, value.Length - 2),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out version) &&
               version > 0;
    }
}
