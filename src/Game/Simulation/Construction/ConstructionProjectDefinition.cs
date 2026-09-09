using System.Collections.Generic;

namespace Game.Simulation.Construction;

public sealed record ConstructionProjectDefinition(
    string Id,
    string Name,
    string Description,
    double IndustryCost,
    IReadOnlyList<string> RequiredTechnologies,
    ConstructionCategory Category,
    double CreditCost = 0.0
);

public enum ConstructionCategory
{
    Science,
    Industry,
    Orbital,
    Ftl,
}
