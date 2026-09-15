using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class SqliteConfigurationTests
{
    [Fact]
    public async Task CreateAsync_EnablesForeignKeyEnforcement()
    {
        await using var database =
            await SqliteTestDatabase.CreateAsync();
        await using var command = database.DbContext.Database
            .GetDbConnection()
            .CreateCommand();

        command.CommandText = "PRAGMA foreign_keys;";

        var result = await command.ExecuteScalarAsync();

        Assert.Equal(
            1,
            Convert.ToInt32(result, CultureInfo.InvariantCulture));
    }
}
