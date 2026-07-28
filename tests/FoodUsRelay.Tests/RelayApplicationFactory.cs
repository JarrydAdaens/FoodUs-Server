using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace FoodUsRelay.Tests;

/// <summary>
/// Boots the real relay host against a throwaway SQLite database in a per-instance temporary
/// directory, so every run exercises migrations from an empty file and leaves nothing behind.
/// </summary>
public sealed class RelayApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseDirectory =
        Path.Combine(Path.GetTempPath(), $"foodus-relay-tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder in_builder)
    {
        Directory.CreateDirectory(_databaseDirectory);
        in_builder.UseSetting("Relay:DatabasePath", Path.Combine(_databaseDirectory, "relay.db"));
    }

    protected override void Dispose(bool in_disposing)
    {
        base.Dispose(in_disposing);

        if (in_disposing && Directory.Exists(_databaseDirectory))
        {
            // Pooled connections keep a handle on the database file open.
            SqliteConnection.ClearAllPools();
            Directory.Delete(_databaseDirectory, recursive: true);
        }
    }
}
