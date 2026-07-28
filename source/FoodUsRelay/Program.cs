using FoodUsRelay.Data;
using FoodUsRelay.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Kestrel's only endpoint is configured in appsettings.json and is pinned to loopback.
// The relay is never internet-facing: a reverse proxy terminates TLS in front of it and
// proxies to that loopback port (wire contract v1 section 2.7). Nothing here redirects to
// or listens on HTTPS, because TLS is not this process's job.
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<SchemaMigrator>();

WebApplication app = builder.Build();

// Migrations run before the first request so the database is never half-shaped under load.
app.Services.GetRequiredService<SchemaMigrator>().ApplyPendingMigrations();

app.MapCapabilitiesEndpoint();

app.Run();

/// <summary>Exposed so the integration tests can boot the real host.</summary>
public partial class Program;
