using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public sealed class ShipbuildingSimulation
{
    private readonly IShipbuildingCapabilityView _capabilityView;

    public ShipbuildingSimulation(IShipbuildingCapabilityView? capabilityView = null)
    {
        _capabilityView = capabilityView ?? new PrototypeShipbuildingCapabilityView();
    }

    public IReadOnlyList<ShipbuildingEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ShipbuildingEvent>();

        foreach (var civilization in galaxy.Civilizations)
        {
            if (civilization.IsSeededAncient)
                continue;

            var state = galaxy.ShipyardStates.First(s => s.CivilizationId == civilization.Id);
            if (state.ActiveDesignId is null && !civilization.IsPlayer)
            {
                var design = SelectAiDesign(galaxy, civilization);
                if (design is not null)
                    TryStartBuild(galaxy, civilization.Id, design.Id, out _);
            }

            if (state.ActiveDesignId is null)
                continue;

            var definition = ShipDesignRegistry.Get(state.ActiveDesignId);
            var economy = galaxy.Economies.First(e => e.CivilizationId == civilization.Id);
            if (economy.Industry <= 0.0)
                continue;

            var remaining = Math.Max(0.0, definition.IndustryCost - state.ActiveBuildProgress);
            var spend = Math.Min(remaining, economy.Industry);
            economy.Industry -= spend;
            state.ActiveBuildProgress += spend;

            if (state.ActiveBuildProgress + 0.0001 < definition.IndustryCost)
                continue;

            // Population reserved when a colony ship was ordered becomes physical cargo on
            // the completed fleet. It must not disappear at the shipbuilding/colonization seam.
            var embarkedPopulation = state.ReservedPopulationMillions;
            var fleet = CreateFleet(galaxy, civilization, definition, embarkedPopulation);
            galaxy.Fleets.Add(fleet);
            state.ActiveDesignId = null;
            state.ActiveBuildProgress = 0.0;
            state.ReservedPopulationMillions = 0.0;
            PromoteNextBuild(state);
            events.Add(new ShipbuildingEvent(civilization.Id, fleet.Id, definition.Id, $"{civilization.Name} completed {fleet.Name}."));
        }

        return events;
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
        if (definition.PopulationCostMillions > 0.0)
        {
            var source = galaxy.Colonies.Where(c => c.CivilizationId == civilizationId).OrderByDescending(c => c.PopulationMillions).FirstOrDefault();
            if (source is null || source.PopulationMillions < definition.PopulationCostMillions + 500.0)
            {
                message = $"At least {definition.PopulationCostMillions + 500.0:0} million population is required before reserving colonists for this ship.";
                return false;
            }

            source.PopulationMillions -= definition.PopulationCostMillions;
            reservedPopulation = definition.PopulationCostMillions;
        }

        if (state.ActiveDesignId is null)
        {
            state.ActiveDesignId = definition.Id;
            state.ActiveBuildProgress = 0.0;
            state.ReservedPopulationMillions = reservedPopulation;
            message = $"Ship construction started: {definition.Name}.";
            return true;
        }

        state.QueuedBuilds.Add(new ShipBuildOrderState
        {
            DesignId = definition.Id,
            ReservedPopulationMillions = reservedPopulation,
        });
        message = $"Queued {definition.Name}. {state.PendingBuildCount}/{ShipyardState.MaxPendingBuilds} pending vessel slots are now in use.";
        return true;
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
    }

    private ShipDesignDefinition? SelectAiDesign(GalaxyState galaxy, CivilizationState civilization)
    {
        var available = GetAvailableDesigns(galaxy, civilization.Id);
        if (available.Count == 0)
            return null;

        var activeFleets = galaxy.Fleets.Where(f => f.IsActive && f.CivilizationId == civilization.Id).ToArray();
        if (!activeFleets.Any(f => f.Role == FleetRole.Scout))
            return available.FirstOrDefault(d => d.Role == FleetRole.Scout);
        if (civilization.Traits.ScientificCuriosity >= 0.60 && !activeFleets.Any(f => f.Role == FleetRole.Science))
            return available.FirstOrDefault(d => d.Role == FleetRole.Science);
        if (civilization.ExpansionAllowed && !activeFleets.Any(f => f.Role == FleetRole.Colony && f.EmbarkedPopulationMillions > 0.0))
            return available.FirstOrDefault(d => d.Role == FleetRole.Colony);

        return null;
    }

    private static FleetState CreateFleet(
        GalaxyState galaxy,
        CivilizationState civilization,
        ShipDesignDefinition definition,
        double embarkedPopulationMillions)
    {
        var home = galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
        var nextId = galaxy.Fleets.Count == 0 ? 0 : galaxy.Fleets.Max(fleet => fleet.Id) + 1;
        var roleCount = galaxy.Fleets.Count(f => f.CivilizationId == civilization.Id && f.Role == definition.Role) + 1;
        var name = definition.Role switch
        {
            FleetRole.Scout => civilization.IsPlayer ? $"Pathfinder {roleCount}" : $"{civilization.Name} Scout {roleCount}",
            FleetRole.Science => civilization.IsPlayer ? $"Discovery {roleCount}" : $"{civilization.Name} Science {roleCount}",
            FleetRole.Colony => civilization.IsPlayer ? $"Pioneer {roleCount}" : $"{civilization.Name} Pioneer {roleCount}",
            _ => $"{civilization.Name} Vessel {roleCount}",
        };

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
            EmbarkedPopulationMillions = definition.Role == FleetRole.Colony
                ? Math.Max(0.0, embarkedPopulationMillions)
                : 0.0,
        };
    }
}

public sealed record ShipbuildingEvent(int CivilizationId, int FleetId, string DesignId, string Message);
public sealed record ShipbuildingOrderResult(bool Accepted, string Message);
