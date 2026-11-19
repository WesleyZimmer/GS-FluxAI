using System.Text.Json;

namespace FluxAI.Api.Api;

public sealed class RunImportRequest
{
    public required string SourceRunId { get; init; }
    public required string ScenarioId { get; init; }
    public required string RunType { get; init; }
    public required string Environment { get; init; }
    public long Seed { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required DateTimeOffset FinishedAtUtc { get; init; }
    public double HorizonSeconds { get; init; }
    public required RunScenarioFingerprint ScenarioFingerprint { get; init; }
    public RunModel? Model { get; init; }
    public JsonElement? EffectiveParameters { get; init; }
    public required TrafficRunSummary TrafficSummary { get; init; }
    public List<MetricPointRequest> Metrics { get; init; } = [];
    public MetricCoverage? Coverage { get; init; }
    public required List<ArtifactReferenceRequest> Artifacts { get; init; }
    public List<string> Limitations { get; init; } = [];
}

public sealed class RunScenarioFingerprint
{
    public required string NetworkSha256 { get; init; }
    public required string CatalogSha256 { get; init; }
    public string? RouteDemandSha256 { get; init; }
    public required string TimingProfileSha256 { get; init; }
}

public sealed class RunModel
{
    public required string Algorithm { get; init; }
    public string? ArtifactId { get; init; }
    public string? Sha256 { get; init; }
}

public sealed class TrafficRunSummary
{
    public long? Departed { get; init; }
    public long? Arrived { get; init; }
    public long? ActiveAtEnd { get; init; }
    public long? DuePendingAtEnd { get; init; }
    public double? MeanCompletedDurationSeconds { get; init; }
    public double? MeanCompletedWaitingSeconds { get; init; }
    public long? CollisionEventsConfirmed { get; init; }
    public long? CollisionVehicleSteps { get; init; }
    public long? TeleportedVehicles { get; init; }
}

public sealed class MetricPointRequest
{
    public required string MetricName { get; init; }
    public double Value { get; init; }
    public required string Unit { get; init; }
    public required MetricScopeRequest Scope { get; init; }
    public int? Episode { get; init; }
    public long? Step { get; init; }
    public double? SimulationTimeSeconds { get; init; }
    public required string SourceKey { get; init; }
}

public sealed class MetricScopeRequest
{
    public required string Type { get; init; }
    public string? SignalControllerId { get; init; }
}

public sealed class MetricCoverage
{
    public long ExpectedSamples { get; init; }
    public long ObservedSamples { get; init; }
    public bool Complete { get; init; }
    public string? Note { get; init; }
}

public sealed class ArtifactReferenceRequest
{
    public required string LogicalName { get; init; }
    public required string Sha256 { get; init; }
    public long? SizeBytes { get; init; }
    public string? ContentType { get; init; }
}

public sealed record RunArtifactResponse(
    Guid Id,
    string LogicalName,
    string Sha256,
    long? SizeBytes,
    string? ContentType);

public sealed record MetricPointResponse(
    string MetricName,
    double Value,
    string Unit,
    MetricScopeRequest Scope,
    int? Episode,
    long? Step,
    double? SimulationTimeSeconds,
    string SourceKey);

public sealed record TrainingRunResponse(
    Guid Id,
    string? SourceRunId,
    string ScenarioId,
    string RunType,
    string Origin,
    string Environment,
    string Status,
    long Seed,
    double HorizonSeconds,
    RunScenarioFingerprint? ScenarioFingerprint,
    RunModel? Model,
    JsonElement EffectiveParameters,
    TrafficRunSummary? TrafficSummary,
    MetricCoverage? MetricCoverage,
    IReadOnlyList<RunArtifactResponse> Artifacts,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    IReadOnlyList<string> Limitations);

public sealed record RunPage(IReadOnlyList<TrainingRunResponse> Items, PageInfo Page);
public sealed record MetricPage(IReadOnlyList<MetricPointResponse> Items, MetricCoverage? Coverage, PageInfo Page);
public sealed record SignalResultResponse(string SignalControllerId, MetricPointResponse Metric, MetricCoverage Coverage);
public sealed record SignalResultPage(IReadOnlyList<SignalResultResponse> Items, PageInfo Page);
