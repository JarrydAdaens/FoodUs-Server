using FoodUsRelay.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodUsRelay.Tests;

/// <summary>
/// The migrator runs on every startup, so re-applying an already-applied script would corrupt
/// the database on the second restart. That makes idempotency the one invariant worth pinning.
/// </summary>
public sealed class SchemaMigratorTests : IDisposable
{
    private readonly string _databaseDirectory =
        Path.Combine(Path.GetTempPath(), $"foodus-relay-migrator-{Guid.NewGuid():N}");

    [Fact]
    public void Applying_migrations_twice_records_each_version_once()
    {
        Directory.CreateDirectory(_databaseDirectory);
        SqliteConnectionFactory connectionFactory = CreateConnectionFactory();
        SchemaMigrator migrator = new(connectionFactory, NullLogger<SchemaMigrator>.Instance);

        migrator.ApplyPendingMigrations();
        migrator.ApplyPendingMigrations();

        using SqliteConnection connection = connectionFactory.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*), COUNT(DISTINCT version) FROM schema_migrations";
        using SqliteDataReader reader = command.ExecuteReader();

        Assert.True(reader.Read());
        int rowCount = reader.GetInt32(0);
        int distinctVersions = reader.GetInt32(1);

        Assert.True(rowCount > 0, "The baseline migration should have been recorded.");
        Assert.Equal(rowCount, distinctVersions);
    }

    private SqliteConnectionFactory CreateConnectionFactory()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Relay:DatabasePath"] = Path.Combine(_databaseDirectory, "relay.db"),
            })
            .Build();

        return new SqliteConnectionFactory(configuration);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, recursive: true);
        }
    }
}
