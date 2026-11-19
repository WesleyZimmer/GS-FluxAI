namespace FluxAI.Api.Domain;

public sealed class TrainingRun
{
    public Guid Id { get; set; }
    public required string SourceRunId { get; set; }
    public required string ScenarioId { get; set; }
    public required string RunType { get; set; }
    public required string Origin { get; set; }
    public required string Environment { get; set; }
    public required string Status { get; set; }
    public long Seed { get; set; }
    public double HorizonSeconds { get; set; }
    public string? ModelJson { get; set; }
    public required string EffectiveParametersJson { get; set; }
    public required string TrafficSummaryJson { get; set; }
    public string? MetricCoverageJson { get; set; }
    public string? ScenarioFingerprintJson { get; set; }
    public required string LimitationsJson { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string PayloadSha256 { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long CreatedAtOrderKey { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
}
