using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Combat;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Shipbuilding;

public sealed class ShipbuildingSimulation
{
    private readonly IShipbuildingCapabilityView _capabilityView;
    private readonly IShipbuildingStrategicPreferenceView? _strategicPreferenceView;

    public ShipbuildingSimulation(
        IShipbuildingCapabilityView? capabilityView = null,
        IShipbuildingStrategicPreferenceView? strategicPreferenceView = null)
    {
        _capabilityView = capabilityView ?? new PrototypeShipbuildingCapabilityView();
        _strategicPreferenceView = strategicPreferenceView;
    }

    public IReadOnlyList<ShipbuildingEvent> Advance(
        GalaxyState galaxy,
        IReadOnlyDictionary<int, double>? industryBudgets = null) =>
        AdvanceCore(galaxy, industryBudgets, null);

    public IReadOnlyList<ShipbuildingEvent> AdvanceForCivilization(GalaxyState galaxy, int civilizationId,
        double industryBudget) => AdvanceCore(galaxy,
            new Dictionary<int, double> { [civilizationId] = industryBudget }, civilizationId);

    private IReadOnlyList<ShipbuildingEvent> AdvanceCore(GalaxyState galaxy,
        IReadOnlyDictionary<int, double>? industryBudgets, int? onlyCivilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (onlyCivilizationId is null) EnsureAutomaticOrders(galaxy);

        var events = new List<ShipbuildingEvent>();

        foreach (var civilization in galaxy.Civilizations)
        {
            if (onlyCivilizationId is int selected && civilization.Id != selected) continue;
            if (civilization.IsSeededAncient)
                continue;

            var state = galaxy.ShipyardStates.First(s => s.CivilizationId == civilization.Id);
            if (state.ActiveDesignId is null)
                continue;

            var definition = ShipDesignRegistry.Get(state.ActiveDesignId);
            var economy = galaxy.Economies.First(e => e.CivilizationId == civilization.Id);
            var remaining = Math.Max(0.0, definition.IndustryCost - state.ActiveBuildProgress);
            var availableIndustry = ResolveBudget(industryBudgets, civilization.Id, economy.Industry);
            var spend = Math.Min(remaining, availableIndustry);
            if (spend <= 0.0 && remaining > 0.0001)
                continue;

            economy.Industry -= spend;
            state.ActiveBuildProgress += spend;

            if (state.ActiveBuildProgress + 0.0001 < definition.IndustryCost)
                continue;

            // Population reserved when a colony ship was ordered becomes physical cargo on
            // the completed fleet. Species identity follows the same conservation chain.
            var embarkedPopulation = state.ReservedPopulationMillions;
            var embarkedPopulationSpeciesId = state.ReservedPopulationSpeciesId;
            var fleet = CreateFleet(
                galaxy,
                civilization,
                definition,
                embarkedPopulation,
                embarkedPopulationSpeciesId);
            galaxy.Fleets.Add(fleet);
            state.ActiveDesignId = null;
            state.ActiveBuildProgress = 0.0;
            state.ReservedPopulationMillions = 0.0;
            state.ReservedPopulationSpeciesId = null;
            PromoteNextBuild(state);
            events.Add(new ShipbuildingEvent(civilization.Id, fleet.Id, definition.Id, $"{civilization.Name} completed {fleet.Name}."));
        }

        return events;
    }

    /// <summary>
    /// Selects missing AI ship orders without spending Industry. Population reservation remains
    /// part of order creation, while Core can resolve shared Industry before production advances.
    /// Strategic preference is advisory: it may fill one currently missing role, but never bypasses
    /// design prerequisites, duplicate-role bounds, expansion policy or colony population rules.
    /// </summary>
    public void EnsureAutomaticOrders(GalaxyState galaxy)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        foreach (var civilization in galaxy.Civilizations)
        {
            if (civilization.IsSeededAncient || civilization.IsPlayer)
                continue;

            var state = galaxy.ShipyardStates.First(s => s.CivilizationId == civilization.Id);
            if (state.ActiveDesignId is not null)
                continue;

            var preference = _strategicPreferenceView?.GetPreference(civilization.Id)
                ?? ShipbuildingStrategicPreference.None;
            var design = SelectAiDesign(galaxy, civilization, preference);
            if (design is not null)
                TryStartBuild(galaxy, civilization.Id, design.Id, out _);
        }
    }

    public double GetIndustryDemand(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var state = galaxy.ShipyardStates.First(s => s.CivilizationId == civilizationId);
        if (state.ActiveDesignId is null)
            return 0.0;

        var definition = ShipDesignRegistry.Get(state.ActiveDesignId);
        return Math.Max(0.0, definition.IndustryCost - state.ActiveBuildProgress);
    }

    public ShipbuildingOrderResult StartBuild(GalaxyState galaxy, int civilizationId, string designId)
    {
        var accepted = TryStartBuild(galaxy, civilizationId, designId, out var message);
        return new ShipbuildingOrderResult(accepted, message);
    }

    public IReadOnlyList<ShipDesignDefinition> GetAvailableDesigns(GalaxyState galaxy, int civilizationId)
    {
        return ShipDesignRegistry.All
            .Where(design => MeetsPrerequisites(galaxy, civilizationId, design.Prerequisites))
            .ToArray();
    }

    private bool MeetsPrerequisites(GalaxyState galaxy, int civilizationId, ShipDesignPrerequisites prerequisites)
    {
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == civilizationId);
        if (prerequisites.RequiredConstructionProjects.Any(projectId => !construction.CompletedProjectIds.Contains(projectId)))
            return false;

        if (prerequisites.AllCivilizationCapabilities.Any(capabilityId => !_capabilityView.HasCivilizationCapability(galaxy, civilizationId, capabilityId)))
            return false;

        return prerequisites.AnyCivilizationCapabilities.Count == 0 ||
               prerequisites.AnyCivilizationCapabilities.Any(capabilityId => _capabilityView.HasCivilizationCapability(galaxy, civilizationId, capabilityId));
    }

    private bool TryStartBuild(GalaxyState galaxy, int civilizationId, string designId, out string message)
    {
        var civilization = galaxy.Civilizations.FirstOrDefault(c => c.Id == civilizationId);
        if (civilization is null)
        {
            message = "Unknown civilization.";
            return false;
        }

        var state = galaxy.ShipyardStates.First(s => s.CivilizationId == civilizationId);
        if (state.PendingBuildCount >= ShipyardState.MaxPendingBuilds)
        {
            message = $"The shipyard queue is full ({ShipyardState.MaxPendingBuilds} pending vessels maximum).";
            return false;
        }

        ShipDesignDefinition definition;
        try { definition = ShipDesignRegistry.Get(designId); }
        catch (InvalidOperationException)
        {
            message = "Unknown ship design.";
            return false;
        }

        if (!MeetsPrerequisites(galaxy, civilizationId, definition.Prerequisites))
        {
            message = "That ship design is not available because its civilization-capability or shipyard prerequisites are not satisfied.";
            return false;
        }

        var reservedPopulation = 0.0;
        string? reservedPopulationSpeciesId = null;
        if (definition.PopulationCostMillions > 0.0)
        {
            var source = galaxy.Colonies
                .Where(c => c.CivilizationId == civilizationId)
                .OrderByDescending(c => c.PopulationMillions)
                .FirstOrDefault();
            if (source is null || source.PopulationMillions < definition.PopulationCostMillions + 500.0)
            {
                message = $"At least {definition.PopulationCostMillions + 500.0:0} million population is required before reserving colonists for this ship.";
                return false;
            }

            if (!SpeciesCatalog.TryGet(source.PopulationSpeciesId, out _))
            {
                message = $"The source colony references unknown population species '{source.PopulationSpeciesId}'.";
                return false;
            }

            source.PopulationMillions -= definition.PopulationCostMillions;
            reservedPopulation = definition.PopulationCostMillions;
            reservedPopulationSpeciesId = source.PopulationSpeciesId;
        }

        if (state.ActiveDesignId is null)
        {
            state.ActiveDesignId = definition.Id;
            state.ActiveBuildProgress = 0.0;
            state.ReservedPopulationMillions = reservedPopulation;
            state.ReservedPopulationSpeciesId = reservedPopulationSpeciesId;
            message = $"Ship construction started: {definition.Name}.";
            return true;
        }

        state.QueuedBuilds.Add(new ShipBuildOrderState
        {
            DesignId = definition.Id,
            ReservedPopulationMillions = reservedPopulation,
            ReservedPopulationSpeciesId = reservedPopulationSpeciesId,
        });
        message = $"Queued {definition.Name}. {state.PendingBuildCount}/{ShipyardState.MaxPendingBuilds} pending vessel slots are now in use.";
        return true;
    }

    private static double ResolveBudget(
        IReadOnlyDictionary<int, double>? industryBudgets,
        int civilizationId,
        double availableIndustry)
    {
        if (industryBudgets is null)
            return availableIndustry;
        if (!industryBudgets.TryGetValue(civilizationId, out var budget))
            return 0.0;
        if (!double.IsFinite(budget))
            throw new ArgumentOutOfRangeException(nameof(industryBudgets), "Industry budgets must be finite.");

        return Math.Min(availableIndustry, Math.Max(0.0, budget));
    }

    private static void PromoteNextBuild(ShipyardState state)
    {
        if (state.QueuedBuilds.Count == 0)
            return;

        var next = state.QueuedBuilds[0];
        state.QueuedBuilds.RemoveAt(0);
        state.ActiveDesignId = next.DesignId;
        state.ActiveBuildProgress = 0.0;
        state.ReservedPopulationMillions = next.ReservedPopulationMillions;
        state.ReservedPopulationSpeciesId = next.ReservedPopulationSpeciesId;
    }

    private ShipDesignDefinition? SelectAiDesign(
        GalaxyState galaxy,
        CivilizationState civilization,
        ShipbuildingStrategicPreference preference)
    {
        var available = GetAvailableDesigns(galaxy, civilization.Id);
        if (available.Count == 0)
            return null;

        var activeFleets = galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == civilization.Id)
            .ToArray();

        if (preference.PreferredNewFleetRole is { } preferredRole &&
            NeedsNewRole(civilization, activeFleets, preferredRole, preference.DeferNewColonization))
        {
            var preferred = available.FirstOrDefault(design => design.Role == preferredRole);
            if (preferred is not null)
                return preferred;
        }

        // Preserve the existing early-release fallback when strategy has no actionable preference.
        if (!activeFleets.Any(fleet => fleet.Role == FleetRole.Scout))
            return available.FirstOrDefault(design => design.Role == FleetRole.Scout);
        if (civilization.Traits.ScientificCuriosity >= 0.60 &&
            !activeFleets.Any(fleet => fleet.Role == FleetRole.Science))
        {
            return available.FirstOrDefault(design => design.Role == FleetRole.Science);
        }

        if (!preference.DeferNewColonization &&
            civilization.ExpansionAllowed &&
            !activeFleets.Any(fleet => fleet.Role == FleetRole.Colony && fleet.EmbarkedPopulationMillions > 0.0))
        {
            return available.FirstOrDefault(design => design.Role == FleetRole.Colony);
        }

        return null;
    }

    private static bool NeedsNewRole(
        CivilizationState civilization,
        IReadOnlyCollection<FleetState> activeFleets,
        FleetRole role,
        bool deferNewColonization) => role switch
    {
        FleetRole.Scout => !activeFleets.Any(fleet => fleet.Role == FleetRole.Scout),
        FleetRole.Science => !activeFleets.Any(fleet => fleet.Role == FleetRole.Science),
        FleetRole.Military => !activeFleets.Any(fleet => fleet.Role == FleetRole.Military),
        FleetRole.Colony => !deferNewColonization &&
                            civilization.ExpansionAllowed &&
                            !activeFleets.Any(fleet =>
                                fleet.Role == FleetRole.Colony &&
                                fleet.EmbarkedPopulationMillions > 0.0),
        _ => false,
    };

    private static FleetState CreateFleet(
        GalaxyState galaxy,
        CivilizationState civilization,
        ShipDesignDefinition definition,
        double embarkedPopulationMillions,
        string? embarkedPopulationSpeciesId)
    {
        var home = galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
        var nextId = galaxy.Fleets.Count == 0 ? 0 : galaxy.Fleets.Max(fleet => fleet.Id) + 1;
        var roleCount = galaxy.Fleets.Count(f => f.CivilizationId == civilization.Id && f.Role == definition.Role) + 1;
        var name = definition.Role switch
        {
            FleetRole.Scout => civilization.IsPlayer ? $"Pathfinder {roleCount}" : $"{civilization.Name} Scout {roleCount}",
            FleetRole.Science => civilization.IsPlayer ? $"Discovery {roleCount}" : $"{civilization.Name} Science {roleCount}",
            FleetRole.Colony => civilization.IsPlayer ? $"Pioneer {roleCount}" : $"{civilization.Name} Pioneer {roleCount}",
            FleetRole.Military => civilization.IsPlayer ? $"Sentinel {roleCount}" : $"{civilization.Name} Patrol {roleCount}",
            _ => $"{civilization.Name} Vessel {roleCount}",
        };

        var isPopulatedColonyShip = definition.Role == FleetRole.Colony && embarkedPopulationMillions > 0.0;
        if (isPopulatedColonyShip &&
            (string.IsNullOrWhiteSpace(embarkedPopulationSpeciesId) || !SpeciesCatalog.TryGet(embarkedPopulationSpeciesId, out _)))
        {
            throw new InvalidOperationException("A populated colony ship must carry a known species identity.");
        }

        return new FleetState
        {
            Id = nextId,
            CivilizationId = civilization.Id,
            Name = name,
            Role = definition.Role,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = definition.StrategicSpeed,
            SensorRange = definition.SensorRange,
            IsActive = true,
            EmbarkedPopulationMillions = isPopulatedColonyShip
                ? Math.Max(0.0, embarkedPopulationMillions)
                : 0.0,
            EmbarkedPopulationSpeciesId = isPopulatedColonyShip
                ? embarkedPopulationSpeciesId
                : null,
            Combat = CombatProfileRegistry.CreateInitialState(definition.CombatProfileId, definition.Role),
        };
    }
}

public sealed record ShipbuildingEvent(int CivilizationId, int FleetId, string DesignId, string Message);
public sealed record ShipbuildingOrderResult(bool Accepted, string Message);
