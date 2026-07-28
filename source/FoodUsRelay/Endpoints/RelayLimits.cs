namespace FoodUsRelay.Endpoints;

/// <summary>
/// The size limits published by <c>GET /v1/capabilities</c> so clients can check before
/// sending. Values are fixed by wire contract v1 section 6.7, not operator-tunable.
/// </summary>
public sealed record RelayLimits(
    int MaxCiphertextBytes,
    int MaxQueuedEnvelopesPerRecipient,
    int MaxDrainBatch,
    int MaxRequestBodyBytes);
