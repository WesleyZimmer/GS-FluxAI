namespace FluxAI.Api.Domain;

public sealed class GreenPhaseOverride
{
    public Guid ConfigurationId { get; set; }
    public int Revision { get; set; }
    public required string ScenarioId { get; set; }
    public required string ControllerId { get; set; }
    public int PhaseIndex { get; set; }
    public double? DecisionMinimumSeconds { get; set; }
    public double? MaximumSeconds { get; set; }
}
