namespace FluxAI.Api.Domain;

public sealed class RunMetric
{
    public Guid Id { get; set; }
    public Guid TrainingRunId { get; set; }
    public required string MetricName { get; set; }
    public double Value { get; set; }
    public required string Unit { get; set; }
    public required string ScopeType { get; set; }
    public string? SignalControllerId { get; set; }
    public int? Episode { get; set; }
    public long? Step { get; set; }
    public double? SimulationTimeSeconds { get; set; }
    public required string SourceKey { get; set; }
}
