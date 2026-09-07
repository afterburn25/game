using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Colonization;

public sealed class ColonizationSimulation
{
    public IReadOnlyList<ColonizationEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ColonizationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.Role == FleetRole.Colony))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);

            if (fleet.DestinationSystemId is null && !civilization.IsPlayer)
                AssignAiColonyDestination(galaxy, fleet, civilization);

            if (fleet.DestinationSystemId is not null || fleet.CurrentSystemId is null)
                continue;

            var systemId = fleet.CurrentSystemId.Value;
            var system = galaxy.Systems.First(s => s.Id == systemId);
            if (!IsColonizable(galaxy, fleet.CivilizationId, system))
                continue;

            var colony = new ColonyState
            {
                Id = galaxy.Colonies.Count == 0 ? 0 : galaxy.Colonies.Max(c => c.Id) + 1,
                CivilizationId = fleet.CivilizationId,
                SystemId = systemId,
                Name = $"{civilization.Name} Colony {galaxy.Colonies.Count(c => c.CivilizationId == civilization.Id) + 1}",
                // Temporary until the integrated shipbuilding population reservation is handed
                // through FleetState. Do not infer/create additional population elsewhere.
                PopulationMillions = 85.0,
                Infrastructure = 0.35,
                Stability = 0.92,
            };

            galaxy.Colonies.Add(colony);
            fleet.IsActive = false;
            fleet.DestinationSystemId = null;

            events.Add(new ColonizationEvent(
                fleet.CivilizationId,
                fleet.Id,
                system.Id,
                colony.Id,
                $"{civilization.Name} established {colony.Name} in {system.Name}."));
        }

        return events;
    }

    public ColonyOrderResult IssuePlayerColonyOrder(GalaxyState galaxy, int civilizationId, int destinationSystemId)
    {
        var system = galaxy.Systems.FirstOrDefault(s => s.Id == destinationSystemId);
        if (system is null)
            return new ColonyOrderResult(false, "Unknown destination.");

        if (!galaxy.Knowledge.IsSystemKnown(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "That astronomical target has not been detected yet.");

        if (!galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "A completed science survey is required before a colony mission can be prepared.");

        // These authoritative facts are consulted only after the acting civilization has
        // legitimately completed the survey that reveals colonization-grade information.
        if (!system.HasHabitableWorld)
            return new ColonyOrderResult(false, "No colonizable habitable world has been found there.");
        if (system.HasPreWarpCivilization)
            return new ColonyOrderResult(false, "A native pre-warp civilization already inhabits this system.");
        if (galaxy.Colonies.Any(c => c.SystemId == destinationSystemId))
            return new ColonyOrderResult(false, "That system is already colonized.");

        var fleet = galaxy.Fleets.FirstOrDefault(f =>
            f.IsActive &&
            f.CivilizationId == civilizationId &&
            f.Role == FleetRole.Colony);
        if (fleet is null)
            return new ColonyOrderResult(false, "No active colony ship is available.");

        fleet.DestinationSystemId = destinationSystemId;
        return new ColonyOrderResult(true, $"{fleet.Name}: colony course set for {system.Name}.");
    }

    private static bool IsColonizable(GalaxyState galaxy, int civilizationId, StarSystemState system) =>
        galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, system.Id) &&
        system.HasHabitableWorld &&
        !system.HasPreWarpCivilization &&
        !galaxy.Colonies.Any(c => c.SystemId == system.Id);

    private static void AssignAiColonyDestination(
        GalaxyState galaxy,
        FleetState fleet,
        CivilizationState civilization)
    {
        var candidate = galaxy.Systems
            .Where(system => IsColonizable(galaxy, civilization.Id, system))
            .Select(system => new
            {
                System = system,
                Distance = Vector2.DistanceSquared(fleet.Position, system.Position),
                Value = (system.HasRareResource ? 14000.0 : 0.0) +
                        (system.Archetype == StarArchetype.HabitableRich ? 10000.0 : 0.0) +
                        civilization.Traits.Territoriality * 6000.0 +
                        civilization.Traits.Greed * (system.HasRareResource ? 9000.0 : 1500.0),
            })
            .OrderByDescending(candidate => candidate.Value - candidate.Distance)
            .ThenBy(candidate => candidate.System.Id)
            .FirstOrDefault();

        if (candidate is not null)
            fleet.DestinationSystemId = candidate.System.Id;
    }
}

public sealed record ColonyOrderResult(bool Accepted, string Message);
public sealed record ColonizationEvent(int CivilizationId, int FleetId, int SystemId, int ColonyId, string Message);
