namespace FoodUsRelay.Data;

/// <summary>One numbered migration script, loaded from an embedded resource.</summary>
/// <param name="Version">The leading number in the script's file name; also its ordering key.</param>
/// <param name="Name">The script's file name, recorded so an applied version is traceable.</param>
/// <param name="Sql">The script body, executed as a single batch inside one transaction.</param>
public sealed record SchemaMigration(int Version, string Name, string Sql);
