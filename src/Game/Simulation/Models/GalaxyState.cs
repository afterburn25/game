using System.Collections.Generic;
using Game.Simulation.Construction;
using Game.Simulation.Knowledge;
using Game.Simulation.Research;

namespace Game.Simulation.Models;

public sealed class GalaxyState
{
    public required long Seed { get; init; }
    public required IReadOnlyList<StarSystemState> Systems { get; init; }
    public required IList<CivilizationState> Civilizations { get; init; }
    public required IList<FleetState> Fleets { get; init; }
    public required IList<ColonyState> Colonies { get; init; }
    public required IReadOnlyList<CivilizationEconomyState> Economies { get; init; }
    public required IList<TechnologyState> Technologies { get; init; }
    public required IList<ConstructionState> ConstructionStates { get; init; }
    public required int PlayerCivilizationId { get; init; }
    public required CivilizationKnowledgeState Knowledge { get; init; }
}
