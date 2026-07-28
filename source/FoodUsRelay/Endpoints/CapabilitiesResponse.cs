namespace FoodUsRelay.Endpoints;

/// <summary>
/// The body of <c>GET /v1/capabilities</c>, shaped exactly as wire contract v1 section 8.7
/// specifies. Fields are only ever added here, never renamed or removed (section 4.1).
/// </summary>
public sealed record CapabilitiesResponse(
    int ContractMajor,
    int ContractMinor,
    IReadOnlyList<int> EnvelopeVersions,
    IReadOnlyList<string> Capabilities,
    RelayLimits Limits,
    int RetentionDays,
    string ServerTime);
