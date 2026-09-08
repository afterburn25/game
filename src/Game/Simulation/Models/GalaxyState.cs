using System.Collections.Generic;
using Game.Simulation.Construction;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Models;

public sealed class GalaxyState
{
    private IReadOnlyList<PlanetaryBodyState>? _planetaryBodies;

    public required long Seed { get; init; }
    public required IReadOnlyList<StarSystemState> Systems { get; init; }

    /// <summary>
    /// Reconstructible deterministic world catalog. Campaign saves already persist Seed and
    /// Systems, so legacy/current saves can regenerate the same bounded planet/moon state
    /// without another save-format field. Supplying an explicit catalog during generation
    /// avoids recomputing it during the active campaign.
    /// </summary>
    public IReadOnlyList<PlanetaryBodyState> PlanetaryBodies
    {
        get => _planetaryBodies ??= new PlanetaryBodyGenerator().Generate(Seed, Systems);
        init => _planetaryBodies = value;
    }

    public required IList<CivilizationState> Civilizations { get; init; }
    public required IList<FleetState> Fleets { get; init; }
    public required IList<ColonyState> Colonies { get; init; }
    public required IReadOnlyList<CivilizationEconomyState> Economies { get; init; }
    public required IList<TechnologyState> Technologies { get; init; }
    public required IList<ConstructionState> ConstructionStates { get; init; }
    public required IList<ShipyardState> ShipyardStates { get; init; }
    public required int PlayerCivilizationId { get; init; }
    public required CivilizationKnowledgeState Knowledge { get; init; }

    /// <summary>
    /// Authoritative persistent diplomacy owner for this campaign. New campaigns start with an
    /// empty bounded state; format-v8 saves may restore the same state without creating a second
    /// diplomacy store in presentation, AI, Combat, or Exploration.
    /// </summary>
    public DiplomacyState Diplomacy { get; init; } = new();
}
