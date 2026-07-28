using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace FoodUsRelay.Data;

/// <summary>
/// Applies the numbered SQL migration scripts embedded in this assembly, in order, recording
/// each applied version in the <c>schema_migrations</c> table. Deliberately the boring shape:
/// sequential scripts plus one bookkeeping row, so later stories add a script and nothing else.
/// </summary>
public sealed class SchemaMigrator
{
    private const string ResourcePrefix = "FoodUsRelay.Data.Migrations.";
    private const string ResourceSuffix = ".sql";

    private static readonly Regex s_versionPattern = new(@"^\D*(\d+)", RegexOptions.Compiled);

    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<SchemaMigrator> _logger;

    public SchemaMigrator(SqliteConnectionFactory in_connectionFactory, ILogger<SchemaMigrator> in_logger)
    {
        _connectionFactory = in_connectionFactory;
        _logger = in_logger;
    }

    /// <summary>
    /// Brings the database up to the latest embedded migration. Safe to run on every startup:
    /// already-applied versions are skipped, so a restart is a no-op.
    /// </summary>
    public void ApplyPendingMigrations()
    {
        using SqliteConnection connection = _connectionFactory.OpenConnection();
        HashSet<int> appliedVersions = ReadAppliedVersions(connection);

        foreach (SchemaMigration migration in LoadMigrations())
        {
            if (appliedVersions.Contains(migration.Version))
            {
                continue;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            Execute(connection, transaction, migration.Sql);
            RecordApplied(connection, transaction, migration);
            transaction.Commit();

            _logger.LogInformation("Applied schema migration {Version} ({Name}).", migration.Version, migration.Name);
        }
    }

    private static IReadOnlyList<SchemaMigration> LoadMigrations()
    {
        Assembly assembly = typeof(SchemaMigrator).Assembly;

        return assembly.GetManifestResourceNames()
            .Where(resourceName => resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && resourceName.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            .Select(resourceName => ReadMigration(assembly, resourceName))
            .OrderBy(migration => migration.Version)
            .ToList();
    }

    private static SchemaMigration ReadMigration(Assembly in_assembly, string in_resourceName)
    {
        string fileName = in_resourceName[ResourcePrefix.Length..];
        Match versionMatch = s_versionPattern.Match(fileName);

        if (!versionMatch.Success)
        {
            throw new InvalidOperationException($"Migration '{fileName}' does not start with a version number.");
        }

        using Stream stream = in_assembly.GetManifestResourceStream(in_resourceName)
            ?? throw new InvalidOperationException($"Migration resource '{in_resourceName}' could not be opened.");
        using StreamReader reader = new(stream);

        int version = int.Parse(versionMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        return new SchemaMigration(version, fileName, reader.ReadToEnd());
    }

    /// <summary>
    /// Returns the versions already applied. The bookkeeping table is itself created by
    /// migration 001, so its absence simply means nothing has been applied yet.
    /// </summary>
    private static HashSet<int> ReadAppliedVersions(SqliteConnection in_connection)
    {
        HashSet<int> versions = [];

        using SqliteCommand tableCheck = in_connection.CreateCommand();
        tableCheck.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'schema_migrations'";

        if (tableCheck.ExecuteScalar() is null)
        {
            return versions;
        }

        using SqliteCommand query = in_connection.CreateCommand();
        query.CommandText = "SELECT version FROM schema_migrations";

        using SqliteDataReader reader = query.ExecuteReader();

        while (reader.Read())
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private static void Execute(SqliteConnection in_connection, SqliteTransaction in_transaction, string in_sql)
    {
        using SqliteCommand command = in_connection.CreateCommand();
        command.Transaction = in_transaction;
        command.CommandText = in_sql;
        command.ExecuteNonQuery();
    }

    private static void RecordApplied(
        SqliteConnection in_connection,
        SqliteTransaction in_transaction,
        SchemaMigration in_migration)
    {
        using SqliteCommand command = in_connection.CreateCommand();
        command.Transaction = in_transaction;
        command.CommandText =
            "INSERT INTO schema_migrations (version, name, applied_at) VALUES ($version, $name, $appliedAt)";
        command.Parameters.AddWithValue("$version", in_migration.Version);
        command.Parameters.AddWithValue("$name", in_migration.Name);
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}
