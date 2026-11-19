using System.Text.Json.Serialization;

namespace FluxAI.Api.Api;

public sealed class ConfigurationWriteRequest
{
    public required string Name { get; init; }
    public required string ScenarioId { get; init; }
    public required GreenTimingRequest GreenTiming { get; init; }
    public required BlockingParameters Blocking { get; init; }
    public PpoParameters? Ppo { get; init; }
}

public sealed class GreenTimingRequest
{
    public required GreenTimingDefault Default { get; init; }
    public List<PhaseTimingOverrideRequest> PhaseOverrides { get; init; } = [];
}

public sealed class GreenTimingDefault
{
    public double DecisionMinimumSeconds { get; init; }
    public double MaximumSeconds { get; init; }
}

public sealed class PhaseTimingOverrideRequest
{
    public required string SignalControllerId { get; init; }
    public int PhaseIndex { get; init; }
    public double? DecisionMinimumSeconds { get; init; }
    public double? MaximumSeconds { get; init; }
}

public sealed class BlockingParameters
{
    public double ObservedCapacityFractionThreshold { get; init; }
    public int ConfirmationSamples { get; init; }
    public double MaximumMeasurementAgeSeconds { get; init; }
}

public sealed class PpoParameters
{
    public int TotalTimesteps { get; init; } = 4096;
    public int CheckpointEvery { get; init; } = 2048;
    public int RolloutSteps { get; init; } = 128;
    public int BatchSize { get; init; } = 64;
    public int Epochs { get; init; } = 10;
    public double LearningRate { get; init; } = 0.0003;
    public double Gamma { get; init; } = 0.99;
    public double GaeLambda { get; init; } = 0.95;
    public double ClipRange { get; init; } = 0.2;
    public double EntropyCoefficient { get; init; } = 0.01;
}

public sealed record EffectivePhaseTiming(
    string SignalControllerId,
    int PhaseIndex,
    double NominalSeconds,
    double RequestedMinimumSeconds,
    double EffectiveMinimumSeconds,
    double MaximumSeconds);

public sealed record ConfigurationResponse(
    Guid Id,
    string Name,
    string ScenarioId,
    int Revision,
    GreenTimingRequest GreenTiming,
    IReadOnlyList<EffectivePhaseTiming> EffectivePhaseTimings,
    BlockingParameters Blocking,
    PpoParameters Ppo,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PageInfo(int PageSize, string? NextPageToken, bool HasMore);
public sealed record ScenarioItem(
    string Id,
    string Name,
    string Revision,
    string NetworkSha256,
    string CatalogSha256,
    string TimingProfileId,
    string Environment,
    int ControllerCount,
    bool Enabled);
public sealed record ScenarioPage(IReadOnlyList<ScenarioItem> Items, PageInfo Page);
public sealed record SignalPhaseResponse(double Duration, string State);
public sealed record SignalControllerItem(
    string Id,
    string ScenarioId,
    bool Adaptive,
    IReadOnlyList<SignalPhaseResponse> Phases);
public sealed record SignalControllerPage(IReadOnlyList<SignalControllerItem> Items, PageInfo Page);

internal sealed record NormalizedConfiguration(
    GreenTimingRequest GreenTiming,
    BlockingParameters Blocking,
    PpoParameters Ppo,
    IReadOnlyList<EffectivePhaseTiming> EffectivePhaseTimings);

internal sealed class SignalPhaseCatalog
{
    [JsonPropertyName("duration")]
    public double Duration { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }
}

internal sealed class CatalogFile
{
    [JsonPropertyName("catalog_revision")]
    public required string CatalogRevision { get; init; }

    [JsonPropertyName("network_sha256")]
    public required string NetworkSha256 { get; init; }

    [JsonPropertyName("signals")]
    public required List<CatalogSignal> Signals { get; init; }
}

internal sealed class CatalogSignal
{
    [JsonPropertyName("controller_id")]
    public required string ControllerId { get; init; }

    [JsonPropertyName("osm_id")]
    public required string OsmId { get; init; }

    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }

    [JsonPropertyName("green_phase_indexes")]
    public required List<int> GreenPhaseIndexes { get; init; }

    [JsonPropertyName("red_clearance_phase_indexes")]
    public required List<int> RedClearancePhaseIndexes { get; init; }

    [JsonPropertyName("phases")]
    public required List<SignalPhaseCatalog> Phases { get; init; }

    [JsonPropertyName("adaptive_control")]
    public bool AdaptiveControl { get; init; }
}

internal sealed class TimingProfileFile
{
    [JsonPropertyName("profile_id")]
    public required string ProfileId { get; init; }
}
