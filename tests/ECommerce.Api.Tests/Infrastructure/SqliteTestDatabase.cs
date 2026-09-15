using ECommerce.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public ECommerceDbContext DbContext { get; }

    private SqliteTestDatabase(
        SqliteConnection connection,
        ECommerceDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
    }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseSqlite(connection)
                .Options;

        var dbContext = new ECommerceDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        return new SqliteTestDatabase(
            connection,
            dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
