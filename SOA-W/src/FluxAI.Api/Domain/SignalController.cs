namespace FluxAI.Api.Domain;

public sealed class SignalController
{
    public required string ScenarioId { get; set; }
    public required string ControllerId { get; set; }
    public required string OsmId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsAdaptive { get; set; }
    public required string GreenPhaseIndexesJson { get; set; }
    public required string RedClearancePhaseIndexesJson { get; set; }
    public required string PhaseCatalogJson { get; set; }
}
