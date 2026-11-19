namespace FluxAI.Api.Domain;

public sealed class Scenario
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Environment { get; set; }
    public required string CatalogRevision { get; set; }
    public required string NetworkSha256 { get; set; }
    public required string CatalogSha256 { get; set; }
    public required string TimingProfileId { get; set; }
    public required string TimingProfileSha256 { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
