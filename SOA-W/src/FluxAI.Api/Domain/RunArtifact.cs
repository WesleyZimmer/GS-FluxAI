namespace FluxAI.Api.Domain;

public sealed class RunArtifact
{
    public Guid Id { get; set; }
    public Guid TrainingRunId { get; set; }
    public required string LogicalName { get; set; }
    public required string Sha256 { get; set; }
    public long? SizeBytes { get; set; }
    public string? ContentType { get; set; }
}
