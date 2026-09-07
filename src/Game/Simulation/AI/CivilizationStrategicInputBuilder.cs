using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.AI;

/// <summary>
/// Builds the civilization's own strategic-state input from authoritative systems while
/// preserving the fair-information boundary for foreign state. Rival information is still
/// supplied separately through KnowledgeSnapshot.
/// </summary>
public sealed class CivilizationStrategicInputBuilder
{
    private readonly IEconomyLogisticsView _logisticsView;
    private readonly IShipbuildingCapabilityView _shipbuildingCapabilities;

    public CivilizationStrategicInputBuilder(
        IEconomyLogisticsView? logisticsView = null,
        IShipbuildingCapabilityView? shipbuildingCapabilities = null)
    {
        _logisticsView = logisticsView ?? new PrototypeEconomyLogisticsView();
        _shipbuildingCapabilities = shipbuildingCapabilities ?? new PrototypeShipbuildingCapabilityView();
    }

    public CivilizationOwnState Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        var economy = galaxy.Economies.FirstOrDefault(state => state.CivilizationId == civilizationId)
            ?? throw new InvalidOperationException($"Civilization {civilizationId} has no economy state.");
        var technology = galaxy.Technologies.FirstOrDefault(state => state.CivilizationId == civilizationId)
            ?? throw new InvalidOperationException($"Civilization {civilizationId} has no technology state.");
        var construction = galaxy.ConstructionStates.FirstOrDefault(state => state.CivilizationId == civilizationId)
            ?? throw new InvalidOperationException($"Civilization {civilizationId} has no construction state.");
        var logistics = _logisticsView.GetSnapshot(galaxy, civilizationId);

        var knownSystemIds = galaxy.Knowledge.GetKnownSystems(civilizationId);
        var ownedColonySystemIds = galaxy.Colonies
            .Where(colony => colony.CivilizationId == civilizationId)
            .Select(colony => colony.SystemId)
            .ToHashSet();

        // Star coordinates/catalog membership are common astronomical knowledge in the current
        // prototype. A detected system is not colonization-grade knowledge: detailed system
        // facts may enter strategic planning only after this civilization completes its survey.
        var hasUnexploredCatalogTargets = knownSystemIds.Count < galaxy.Systems.Count;
        var hasKnownColonizationOpportunity = galaxy.Systems.Any(system =>
            galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, system.Id)
            && system.HasHabitableWorld
            && !system.HasPreWarpCivilization
            && !ownedColonySystemIds.Contains(system.Id)
            && !galaxy.Colonies.Any(colony => colony.SystemId == system.Id));

        var hasSpacecraftConstruction = _shipbuildingCapabilities.HasCivilizationCapability(
            galaxy,
            civilizationId,
            ShipbuildingCapabilityIds.SpacecraftConstruction);
        var hasExperimentalTransit = _shipbuildingCapabilities.HasCivilizationCapability(
            galaxy,
            civilizationId,
            ShipbuildingCapabilityIds.ExperimentalInterstellarTransit);
        var hasOrbitalShipyard = construction.CompletedProjectIds.Contains("orbital_shipyard");
        var canBuildInterstellarShips = hasSpacecraftConstruction && hasExperimentalTransit && hasOrbitalShipyard;

        var activeFleets = galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == civilizationId)
            .ToArray();
        var militaryFleetCount = activeFleets.Count(fleet => fleet.Role == FleetRole.Military);
        var civilianFleetCount = activeFleets.Length - militaryFleetCount;

        // Current prototype lacks combat-value stats. Keep this explicitly coarse and own-state
        // only so the planner can be upgraded later without changing its fair-information contract.
        var ownMilitaryStrength = militaryFleetCount * 100.0 + civilianFleetCount * 8.0;
        var colonyCount = galaxy.Colonies.Count(colony => colony.CivilizationId == civilizationId);
        var desiredMilitaryFleets = canBuildInterstellarShips ? Math.Max(1, (int)Math.Ceiling(colonyCount / 2.0)) : 0;
        var fleetCapacityShortfall = militaryFleetCount < desiredMilitaryFleets;

        var availableResearch = technology.ActiveResearchId is null
            && TechnologyRegistry.GetAvailable(technology, construction).Count > 0;

        // Science throughput is the closest current runtime proxy for usable research capacity.
        // The Adaptive Research runtime can replace this input source later without changing
        // CivilizationOwnState or the strategic planner.
        var researchCapacity = Math.Max(0.0, economy.LastSciencePerSecond);

        return new CivilizationOwnState(
            MilitaryStrength: Math.Max(1.0, ownMilitaryStrength),
            SupplyCoverageRatio: logistics.EffectiveCoverageRatio,
            IndustryReserve: Math.Max(0.0, economy.Industry),
            ResearchCapacity: researchCapacity,
            HasAvailableResearch: availableResearch,
            HasUnexploredReachableSystems: hasUnexploredCatalogTargets && hasExperimentalTransit,
            HasKnownColonizationOpportunity: hasKnownColonizationOpportunity && hasExperimentalTransit,
            CanBuildInterstellarShips: canBuildInterstellarShips,
            HasFleetCapacityShortfall: fleetCapacityShortfall);
    }
}
