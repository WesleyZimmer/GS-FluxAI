namespace FluxAI.Api.Domain;

public sealed class TrainingConfigurationRevision
{
    public Guid ConfigurationId { get; set; }
    public int Revision { get; set; }
    public required string GreenTimingJson { get; set; }
    public required string BlockingJson { get; set; }
    public required string PpoJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
