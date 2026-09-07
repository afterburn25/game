using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Exploration;
using Game.Simulation.Models;

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

            // During the compatibility phase there is exactly one deterministic viable colony
            // candidate per legacy-habitable system. The body can therefore be reconstructed
            // from campaign seed + system after save/load without introducing redundant v7 state.
            if (fleet.DestinationSystemId is null && fleet.CurrentSystemId is int currentSystemId)
            {
                var targetBody = SelectCompatibilityCandidate(galaxy, currentSystemId);
                if (fleet.EmbarkedPopulationMillions > 0.0 &&
                    targetBody is not null &&
                    IsColonizable(galaxy, fleet.CivilizationId, targetBody))
                {
                    var colonists = fleet.EmbarkedPopulationMillions;
                    var colony = new ColonyState
                    {
                        Id = galaxy.Colonies.Count == 0 ? 0 : galaxy.Colonies.Max(c => c.Id) + 1,
                        CivilizationId = fleet.CivilizationId,
                        SystemId = currentSystemId,
                        Name = $"{civilization.Name} Colony {galaxy.Colonies.Count(c => c.CivilizationId == civilization.Id) + 1}",
                        PopulationMillions = colonists,
                        Infrastructure = 0.35,
                        Stability = 0.92,
                    };

                    galaxy.Colonies.Add(colony);
                    fleet.EmbarkedPopulationMillions = 0.0;
                    fleet.IsActive = false;
                    fleet.DestinationSystemId = null;

                    events.Add(new ColonizationEvent(
                        fleet.CivilizationId,
                        fleet.Id,
                        currentSystemId,
                        colony.Id,
                        $"{civilization.Name} established {colony.Name} on {targetBody.Name} with {colonists:0.0} million colonists."));
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
        var body = SelectCompatibilityCandidate(galaxy, destinationSystemId);
        return body is null
            ? ValidateSystemWithoutCandidate(galaxy, civilizationId, destinationSystemId)
            : IssuePlayerColonyOrder(galaxy, civilizationId, destinationSystemId, body.Id);
    }

    /// <summary>
    /// Body-aware colony command. The current compatibility model intentionally approves only
    /// the unique legacy candidate. Once Species is integrated and multiple bodies can be
    /// contextually viable, the accepted body ID will need an explicit shared save-schema field.
    /// </summary>
    public ColonyOrderResult IssuePlayerColonyOrder(
        GalaxyState galaxy,
        int civilizationId,
        int destinationSystemId,
        int planetaryBodyId)
    {
        var system = galaxy.Systems.FirstOrDefault(s => s.Id == destinationSystemId);
        if (system is null)
            return new ColonyOrderResult(false, "Unknown destination.");

        if (!galaxy.Knowledge.IsSystemKnown(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "That astronomical target has not been detected yet.");

        if (!galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "A completed science survey is required before a colony mission can be prepared.");

        var body = galaxy.PlanetaryBodies.FirstOrDefault(candidate =>
            candidate.Id == planetaryBodyId && candidate.SystemId == destinationSystemId);
        if (body is null)
            return new ColonyOrderResult(false, "That planetary body is not part of the surveyed destination system.");

        if (!body.LegacyColonizationCandidate)
        {
            return new ColonyOrderResult(
                false,
                "This world's physical environment is known, but species-relative habitability is not integrated yet; it cannot be approved by the temporary compatibility colony model.");
        }
        if (!body.Environment.HasSolidSurface)
            return new ColonyOrderResult(false, "The selected body has no solid settlement surface in the current colony model.");
        if (body.HasPreWarpCivilization)
            return new ColonyOrderResult(false, "A native pre-warp civilization already inhabits the selected world.");
        if (galaxy.Colonies.Any(c => c.SystemId == destinationSystemId))
            return new ColonyOrderResult(false, "That system already contains a founded colony in the current single-colony early-release model.");

        var fleet = galaxy.Fleets.FirstOrDefault(f =>
            f.IsActive &&
            f.CivilizationId == civilizationId &&
            f.Role == FleetRole.Colony &&
            f.EmbarkedPopulationMillions > 0.0);
        if (fleet is null)
            return new ColonyOrderResult(false, "No colony ship carrying reserved colonists is available.");

        var reach = AssessOperationalReach(galaxy, fleet, destinationSystemId);
        if (!reach.IsSupported)
            return new ColonyOrderResult(false, reach.Reason);

        fleet.DestinationSystemId = destinationSystemId;
        return new ColonyOrderResult(
            true,
            $"{fleet.Name}: colony course set for {body.Name} in {system.Name} with {fleet.EmbarkedPopulationMillions:0.0} million colonists aboard.");
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

    /// <summary>
    /// Resolves the physical world occupied by a current compatibility-phase colony. This is
    /// deterministic and reconstructible because the current model permits one colony candidate
    /// per system. It must not be used once multiple species-relative body choices are enabled.
    /// </summary>
    public PlanetaryBodyState? ResolveCompatibilityColonyWorld(GalaxyState galaxy, ColonyState colony) =>
        SelectCompatibilityCandidate(galaxy, colony.SystemId);

    private MissionReachAssessment AssessOperationalReach(GalaxyState galaxy, FleetState fleet, int destinationSystemId) =>
        _operationalReach.Assess(
            galaxy,
            fleet.CivilizationId,
            fleet,
            destinationSystemId,
            InterstellarMissionKind.Colony);

    private static bool IsColonizable(GalaxyState galaxy, int civilizationId, PlanetaryBodyState body) =>
        galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, body.SystemId) &&
        body.LegacyColonizationCandidate &&
        body.Environment.HasSolidSurface &&
        !body.HasPreWarpCivilization &&
        !galaxy.Colonies.Any(c => c.SystemId == body.SystemId);

    private void AssignAiColonyDestination(
        GalaxyState galaxy,
        FleetState fleet,
        CivilizationState civilization)
    {
        var systemsById = galaxy.Systems.ToDictionary(system => system.Id);
        var candidate = galaxy.PlanetaryBodies
            .Where(body => IsColonizable(galaxy, civilization.Id, body))
            .Where(body => AssessOperationalReach(galaxy, fleet, body.SystemId).IsSupported)
            .Select(body => new
            {
                Body = body,
                System = systemsById[body.SystemId],
                Distance = Vector2.DistanceSquared(fleet.Position, systemsById[body.SystemId].Position),
                Value = 10000.0 +
                        (body.HasRareResource ? 14000.0 : 0.0) +
                        civilization.Traits.Territoriality * 6000.0 +
                        civilization.Traits.Greed * (body.HasRareResource ? 9000.0 : 1500.0),
            })
            .OrderByDescending(candidate => candidate.Value - candidate.Distance)
            .ThenBy(candidate => candidate.System.Id)
            .ThenBy(candidate => candidate.Body.Id)
            .FirstOrDefault();

        if (candidate is not null)
            fleet.DestinationSystemId = candidate.System.Id;
    }

    private static PlanetaryBodyState? SelectCompatibilityCandidate(GalaxyState galaxy, int systemId) =>
        galaxy.PlanetaryBodies
            .Where(body => body.SystemId == systemId)
            .OrderBy(body => body.Id)
            .FirstOrDefault(body => body.LegacyColonizationCandidate && body.Environment.HasSolidSurface);

    private static ColonyOrderResult ValidateSystemWithoutCandidate(
        GalaxyState galaxy,
        int civilizationId,
        int destinationSystemId)
    {
        var system = galaxy.Systems.FirstOrDefault(candidate => candidate.Id == destinationSystemId);
        if (system is null)
            return new ColonyOrderResult(false, "Unknown destination.");
        if (!galaxy.Knowledge.IsSystemKnown(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "That astronomical target has not been detected yet.");
        if (!galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "A completed science survey is required before a colony mission can be prepared.");

        return new ColonyOrderResult(
            false,
            "No compatibility colony candidate exists in this surveyed system. Other worlds may become viable once species-relative habitability and environmental mitigation are integrated.");
    }
}

public sealed record ColonyOrderResult(bool Accepted, string Message);
public sealed record ColonizationEvent(int CivilizationId, int FleetId, int SystemId, int ColonyId, string Message);
