using System.Globalization;
using System.Net;
using System.Text.Json;

namespace FoodUsRelay.Tests;

/// <summary>
/// The story's smoke test: the host boots, migrations apply against a throwaway database,
/// and the one wire-facing endpoint answers in the exact shape of wire contract v1 section 8.7.
/// </summary>
public sealed class CapabilitiesEndpointTests : IClassFixture<RelayApplicationFactory>
{
    private readonly RelayApplicationFactory _factory;

    public CapabilitiesEndpointTests(RelayApplicationFactory in_factory)
    {
        _factory = in_factory;
    }

    [Fact]
    public async Task Capabilities_returns_the_contract_shaped_response()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/v1/capabilities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement body = document.RootElement;

        string[] expectedFields =
        [
            "contractMajor",
            "contractMinor",
            "envelopeVersions",
            "capabilities",
            "limits",
            "retentionDays",
            "serverTime",
        ];
        Assert.Equal(expectedFields, body.EnumerateObject().Select(property => property.Name).ToArray());

        Assert.Equal(1, body.GetProperty("contractMajor").GetInt32());
        Assert.Equal(0, body.GetProperty("contractMinor").GetInt32());
        Assert.Equal(30, body.GetProperty("retentionDays").GetInt32());
        Assert.Equal(JsonValueKind.Array, body.GetProperty("envelopeVersions").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("capabilities").ValueKind);

        JsonElement limits = body.GetProperty("limits");
        Assert.Equal(262144, limits.GetProperty("maxCiphertextBytes").GetInt32());
        Assert.Equal(1000, limits.GetProperty("maxQueuedEnvelopesPerRecipient").GetInt32());
        Assert.Equal(100, limits.GetProperty("maxDrainBatch").GetInt32());
        Assert.Equal(393216, limits.GetProperty("maxRequestBodyBytes").GetInt32());

        // serverTime is RFC 3339 UTC with a Z suffix and second precision (section 6.5).
        string serverTime = body.GetProperty("serverTime").GetString()!;
        Assert.True(DateTimeOffset.TryParseExact(
            serverTime,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out _));
    }

    /// <summary>
    /// The app hides or greys every relay-backed feature the relay does not report, so a
    /// capability claimed before it exists would break graceful degradation. This skeleton
    /// ships no domain endpoint, so both lists must still be empty.
    /// </summary>
    [Fact]
    public async Task Capabilities_reports_nothing_this_relay_cannot_yet_do()
    {
        using HttpClient client = _factory.CreateClient();

        using JsonDocument document = JsonDocument.Parse(
            await client.GetStringAsync("/v1/capabilities"));

        Assert.Empty(document.RootElement.GetProperty("capabilities").EnumerateArray());
        Assert.Empty(document.RootElement.GetProperty("envelopeVersions").EnumerateArray());
    }
}
