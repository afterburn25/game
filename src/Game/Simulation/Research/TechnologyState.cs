using System.Collections.Generic;

namespace Game.Simulation.Research;

public sealed class TechnologyState
{
    public required int CivilizationId { get; init; }
    public HashSet<string> CompletedTechnologyIds { get; } = new();
    public string? ActiveResearchId { get; set; }
    public double ActiveResearchProgress { get; set; }
}
