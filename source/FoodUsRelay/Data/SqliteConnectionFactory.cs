using Microsoft.Data.Sqlite;

namespace FoodUsRelay.Data;

/// <summary>
/// Opens connections to the relay's single SQLite database, whose location comes from
/// configuration so the droplet can point it at a real data directory without a code change.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private const string DatabasePathKey = "Relay:DatabasePath";

    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration in_configuration)
    {
        string? databasePath = in_configuration[DatabasePathKey];

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new InvalidOperationException($"Configuration value '{DatabasePathKey}' is missing or blank.");
        }

        // Built through the builder rather than by string concatenation so a path value can
        // never inject additional connection-string keywords (laws.md, Data Access).
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
    }

    /// <summary>Opens a new connection. The caller owns it and must dispose it.</summary>
    public SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }
}
