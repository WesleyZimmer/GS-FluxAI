namespace FluxAI.Api.Domain;

public sealed class TrainingConfiguration
{
    public Guid Id { get; set; }
    public required string ScenarioId { get; set; }
    public required string Name { get; set; }
    public int CurrentRevision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
