using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Exploration;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Colonization;

public sealed class ColonizationSimulation
{
    private readonly IInterstellarOperationalReachView _operationalReach;

    public ColonizationSimulation(IInterstellarOperationalReachView? operationalReach = null)
    {
        _operationalReach = operationalReach ?? new PrototypeInterstellarOperationalReachView();
    }

    public IReadOnlyList<ColonizationEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ColonizationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.Role == FleetRole.Colony))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);

            // Found first when a colony ship has already arrived. AI previously selected a
            // destination before checking its current system, which could repeatedly reassign
            // the same zero-distance target and prevent settlement establishment.
            if (fleet.DestinationSystemId is null && fleet.CurrentSystemId is int currentSystemId)
            {
                var currentSystem = galaxy.Systems.First(s => s.Id == currentSystemId);
                if (fleet.EmbarkedPopulationMillions > 0.0 &&
                    IsColonizable(galaxy, fleet.CivilizationId, currentSystem))
                {
                    var colonists = fleet.EmbarkedPopulationMillions;
                    var colonistSpeciesId = RequireEmbarkedPopulationSpecies(fleet);
                    var colony = new ColonyState
                    {
                        Id = galaxy.Colonies.Count == 0 ? 0 : galaxy.Colonies.Max(c => c.Id) + 1,
                        CivilizationId = fleet.CivilizationId,
                        SystemId = currentSystemId,
                        Name = $"{civilization.Name} Colony {galaxy.Colonies.Count(c => c.CivilizationId == civilization.Id) + 1}",
                        PopulationSpeciesId = colonistSpeciesId,
                        PopulationMillions = colonists,
                        Infrastructure = 0.35,
                        Stability = 0.92,
                    };

                    galaxy.Colonies.Add(colony);
                    fleet.EmbarkedPopulationMillions = 0.0;
                    fleet.EmbarkedPopulationSpeciesId = null;
                    fleet.IsActive = false;
                    fleet.DestinationSystemId = null;

                    events.Add(new ColonizationEvent(
                        fleet.CivilizationId,
                        fleet.Id,
                        currentSystem.Id,
                        colony.Id,
                        $"{civilization.Name} established {colony.Name} in {currentSystem.Name} with {colonists:0.0} million {SpeciesCatalog.Get(colonistSpeciesId).DisplayName} colonists."));
                    continue;
                }
            }

            if (fleet.DestinationSystemId is null &&
                !civilization.IsPlayer &&
                fleet.EmbarkedPopulationMillions > 0.0)
            {
                AssignAiColonyDestination(galaxy, fleet, civilization);
            }
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
            f.Role == FleetRole.Colony &&
            f.EmbarkedPopulationMillions > 0.0);
        if (fleet is null)
            return new ColonyOrderResult(false, "No colony ship carrying reserved colonists is available.");

        if (string.IsNullOrWhiteSpace(fleet.EmbarkedPopulationSpeciesId) ||
            !SpeciesCatalog.TryGet(fleet.EmbarkedPopulationSpeciesId, out var embarkedSpecies))
        {
            return new ColonyOrderResult(false, "The colony ship's passenger species identity is invalid.");
        }

        var reach = AssessOperationalReach(galaxy, fleet, destinationSystemId);
        if (!reach.IsSupported)
            return new ColonyOrderResult(false, reach.Reason);

        fleet.DestinationSystemId = destinationSystemId;
        return new ColonyOrderResult(
            true,
            $"{fleet.Name}: colony course set for {system.Name} with {fleet.EmbarkedPopulationMillions:0.0} million {embarkedSpecies.DisplayName} colonists aboard.");
    }

    public MissionReachAssessment AssessOperationalReach(GalaxyState galaxy, int fleetId, int destinationSystemId)
    {
        var fleet = galaxy.Fleets.FirstOrDefault(f =>
            f.Id == fleetId &&
            f.IsActive &&
            f.Role == FleetRole.Colony &&
            f.EmbarkedPopulationMillions > 0.0);
        return fleet is null
            ? MissionReachAssessment.Unsupported("No populated colony ship is available.")
            : AssessOperationalReach(galaxy, fleet, destinationSystemId);
    }

    private MissionReachAssessment AssessOperationalReach(GalaxyState galaxy, FleetState fleet, int destinationSystemId) =>
        _operationalReach.Assess(
            galaxy,
            fleet.CivilizationId,
            fleet,
            destinationSystemId,
            InterstellarMissionKind.Colony);

    private static bool IsColonizable(GalaxyState galaxy, int civilizationId, StarSystemState system) =>
        galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, system.Id) &&
        system.HasHabitableWorld &&
        !system.HasPreWarpCivilization &&
        !galaxy.Colonies.Any(c => c.SystemId == system.Id);

    private void AssignAiColonyDestination(
        GalaxyState galaxy,
        FleetState fleet,
        CivilizationState civilization)
    {
        var candidate = galaxy.Systems
            .Where(system => IsColonizable(galaxy, civilization.Id, system))
            .Where(system => AssessOperationalReach(galaxy, fleet, system.Id).IsSupported)
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

    private static string RequireEmbarkedPopulationSpecies(FleetState fleet)
    {
        if (fleet.EmbarkedPopulationMillions <= 0.0)
            throw new InvalidOperationException($"Fleet {fleet.Id} has no embarked population to identify.");

        var speciesId = fleet.EmbarkedPopulationSpeciesId;
        if (string.IsNullOrWhiteSpace(speciesId) || !SpeciesCatalog.TryGet(speciesId, out _))
        {
            throw new InvalidOperationException(
                $"Fleet {fleet.Id} carries population without a valid species identity '{speciesId}'.");
        }

        return speciesId;
    }
}

public sealed record ColonyOrderResult(bool Accepted, string Message);
public sealed record ColonizationEvent(int CivilizationId, int FleetId, int SystemId, int ColonyId, string Message);
