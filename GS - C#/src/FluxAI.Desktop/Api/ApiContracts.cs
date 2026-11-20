using System.Net;
using System.Text.Json;

namespace FluxAI.Desktop.Api;

internal sealed record PageInfo(int PageSize, string? NextPageToken, bool HasMore);

internal sealed record ScenarioItem(
    string Id,
    string Name,
    string Revision,
    string Environment,
    int ControllerCount,
    bool Enabled);

internal sealed record ScenarioPage(IReadOnlyList<ScenarioItem> Items, PageInfo Page);

internal sealed record SignalPhase(double Duration, string State);

internal sealed record SignalControllerItem(
    string Id,
    string ScenarioId,
    bool Adaptive,
    IReadOnlyList<SignalPhase> Phases);

internal sealed record SignalControllerPage(
    IReadOnlyList<SignalControllerItem> Items,
    PageInfo Page);

internal sealed record GreenTimingDefault(double DecisionMinimumSeconds, double MaximumSeconds);

internal sealed record GreenTimingWrite(
    GreenTimingDefault Default,
    IReadOnlyList<object> PhaseOverrides);

internal sealed record BlockingWrite(
    double ObservedCapacityFractionThreshold,
    int ConfirmationSamples,
    double MaximumMeasurementAgeSeconds);

internal sealed record PpoWrite(
    int TotalTimesteps,
    int CheckpointEvery,
    int RolloutSteps,
    int BatchSize,
    int Epochs,
    double LearningRate,
    double Gamma,
    double GaeLambda,
    double ClipRange,
    double EntropyCoefficient);

internal sealed record ConfigurationWriteRequest(
    string Name,
    string ScenarioId,
    GreenTimingWrite GreenTiming,
    BlockingWrite Blocking,
    PpoWrite Ppo);

internal sealed record ConfigurationResponse(
    Guid Id,
    string Name,
    string ScenarioId,
    int Revision,
    GreenTimingWrite GreenTiming,
    IReadOnlyList<EffectivePhaseTiming> EffectivePhaseTimings,
    BlockingWrite Blocking,
    PpoWrite Ppo);

internal sealed record EffectivePhaseTiming(
    string SignalControllerId,
    int PhaseIndex,
    double NominalSeconds,
    double RequestedMinimumSeconds,
    double EffectiveMinimumSeconds,
    double MaximumSeconds);

internal sealed record RunRequest(
    Guid ConfigurationId,
    string RunType,
    long Seed,
    int HorizonSeconds);

internal sealed record RunProgress(
    string Phase,
    int? Episode,
    long? CompletedTimesteps,
    long? TotalTimesteps,
    DateTimeOffset? UpdatedAtUtc = null,
    int? Sequence = null,
    string? Note = null);

internal sealed record RunFailure(string Code, string Message, string? LogArtifactId = null);

internal sealed record RunScenarioFingerprint(
    string NetworkSha256,
    string CatalogSha256,
    string? RouteDemandSha256,
    string TimingProfileSha256);

internal sealed record RunModel(string Algorithm, string? ArtifactId, string? Sha256);

internal sealed record TrafficRunSummary(
    long? Departed,
    long? Arrived,
    long? ActiveAtEnd,
    long? DuePendingAtEnd,
    double? MeanCompletedDurationSeconds,
    double? MeanCompletedWaitingSeconds,
    long? CollisionEventsConfirmed,
    long? CollisionVehicleSteps,
    long? TeleportedVehicles);

internal sealed record MetricCoverage(long ExpectedSamples, long ObservedSamples, bool Complete, string? Note);

internal sealed record RunArtifactItem(Guid Id, string LogicalName, string Sha256, long? SizeBytes, string? ContentType);

internal sealed record MetricScope(string Type, string? SignalControllerId);

internal sealed record MetricPoint(
    string MetricName,
    double Value,
    string Unit,
    MetricScope Scope,
    int? Episode,
    long? Step,
    double? SimulationTimeSeconds,
    string SourceKey);

internal sealed record MetricPage(IReadOnlyList<MetricPoint> Items, MetricCoverage? Coverage, PageInfo Page);

internal sealed record SignalResultItem(string SignalControllerId, MetricPoint Metric, MetricCoverage Coverage);

internal sealed record SignalResultPage(IReadOnlyList<SignalResultItem> Items, PageInfo Page);

internal sealed record TrainingRun(
    Guid Id,
    string? SourceRunId,
    string ScenarioId,
    Guid? ConfigurationId,
    int? ConfigurationRevision,
    string RunType,
    string Origin,
    string Environment,
    string Status,
    long Seed,
    double HorizonSeconds,
    JsonElement EffectiveParameters,
    RunScenarioFingerprint? ScenarioFingerprint,
    RunModel? Model,
    TrafficRunSummary? TrafficSummary,
    MetricCoverage? MetricCoverage,
    IReadOnlyList<RunArtifactItem>? Artifacts,
    IReadOnlyList<string>? Limitations,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    RunProgress? Progress = null,
    RunFailure? Failure = null);

internal sealed record RunPage(IReadOnlyList<TrainingRun> Items, PageInfo Page);

internal sealed class ApiRequestException(HttpStatusCode statusCode, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
