using System.Collections.Generic;

namespace Game.Simulation.Research;

public sealed record TechnologyDefinition(
    string Id,
    string Name,
    string Description,
    double ResearchCost,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<string> RequiredProjects,
    TechnologyCategory Category
);

public enum TechnologyCategory
{
    Industry,
    Propulsion,
    Sensors,
    Physics,
    Ftl,
}
