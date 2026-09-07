using System.Collections.Generic;

namespace Game.Simulation.Construction;

public sealed class ConstructionState
{
    public required int CivilizationId { get; init; }
    public HashSet<string> CompletedProjectIds { get; } = new();
    public string? ActiveProjectId { get; set; }
    public double ActiveProjectProgress { get; set; }
}
