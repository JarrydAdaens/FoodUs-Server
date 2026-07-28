using System.Globalization;

namespace FoodUsRelay.Endpoints;

/// <summary>
/// <c>GET /v1/capabilities</c> — the version and capability query of wire contract v1
/// section 8.7. Unauthenticated by design (section 5.7): a client must be able to discover
/// what the relay supports before it has any relationship with it. It doubles as the
/// liveness signal used by deployment checks and the app's relay-URL connection check.
/// </summary>
public static class CapabilitiesEndpoint
{
    private const int ContractMajor = 1;
    private const int ContractMinor = 0;
    private const int RetentionDays = 30;

    private static readonly RelayLimits s_limits = new(
        MaxCiphertextBytes: 262144,
        MaxQueuedEnvelopesPerRecipient: 1000,
        MaxDrainBatch: 100,
        MaxRequestBodyBytes: 393216);

    public static void MapCapabilitiesEndpoint(this IEndpointRouteBuilder in_routes)
    {
        in_routes.MapGet("/v1/capabilities", () => Results.Ok(BuildResponse()));
    }

    private static CapabilitiesResponse BuildResponse()
    {
        return new CapabilitiesResponse(
            ContractMajor: ContractMajor,
            ContractMinor: ContractMinor,

            // Both lists report only what this relay can actually do today. No push endpoint
            // exists yet, so no envelope version is accepted, and no capability is claimed.
            // Each of Milestone 3's Stories 3-5 appends its own name as it ships, matching the
            // project's "server leads, app follows" rule. Over-reporting here would break the
            // app's graceful degradation, which trusts this list literally.
            EnvelopeVersions: [],
            Capabilities: [],

            // Contract-fixed policy values (sections 6.7 and 10). They describe the contract
            // this relay implements; the code that enforces them ships with Stories 3-5.
            Limits: s_limits,
            RetentionDays: RetentionDays,

            ServerTime: DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }
}
