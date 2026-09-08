using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

/// <summary>
/// Coordinates AI-owned survey fleets without creating persistent assignment state. Existing live
/// destinations and local survey work are the reservation source of truth. A fleet prefers an
/// unreserved supported target, but may share a target when the bounded planning window contains
/// no other supported work so coordination never manufactures unnecessary idleness.
/// </summary>
public sealed class ExplorationAiMissionCoordinator
{
    private readonly ExplorationMissionPlanner _missionPlanner;

    public ExplorationAiMissionCoordinator(ExplorationMissionPlanner missionPlanner)
    {
        _missionPlanner = missionPlanner ?? throw new ArgumentNullException(nameof(missionPlanner));
    }

    public ExplorationAiMissionSelection SelectMission(GalaxyState galaxy, FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);

        if (!fleet.IsActive || fleet.Role is not (FleetRole.Scout or FleetRole.Science))
        {
            return ExplorationAiMissionSelection.None(
                fleet.Id,
                "Only active scout/science fleets participate in AI exploration coordination.");
        }

        var plan = _missionPlanner.BuildPlan(
            galaxy,
            fleet.Id,
            ExplorationMissionPlanner.HardMaximumCandidates);
        var supported = plan.Candidates.Where(candidate => candidate.Reach.IsSupported).ToArray();
        if (supported.Length == 0)
            return ExplorationAiMissionSelection.None(fleet.Id, "No supported survey work is currently available.");

        var reservations = GetReservationSet(galaxy, fleet);
        var unique = supported.FirstOrDefault(candidate => !reservations.Contains(candidate.SystemId));
        var selected = unique ?? supported[0];
        var usedSharedFallback = unique is null;

        return new ExplorationAiMissionSelection(
            fleet.Id,
            selected,
            usedSharedFallback,
            reservations.OrderBy(systemId => systemId).ToArray(),
            usedSharedFallback
                ? $"All supported targets in the bounded planning window are already reserved; sharing {selected.CatalogName}."
                : $"Selected unreserved target {selected.CatalogName}.");
    }

    private static HashSet<int> GetReservationSet(GalaxyState galaxy, FleetState requestingFleet)
    {
        var reserved = new HashSet<int>();
        foreach (var other in galaxy.Fleets)
        {
            if (!other.IsActive ||
                other.Id == requestingFleet.Id ||
                other.CivilizationId != requestingFleet.CivilizationId ||
                other.Role is not (FleetRole.Scout or FleetRole.Science))
            {
                continue;
            }

            if (other.DestinationSystemId is int destinationSystemId)
            {
                reserved.Add(destinationSystemId);
                continue;
            }

            if (other.CurrentSystemId is int currentSystemId &&
                ExplorationMissionPlanner.NeedsSurveyWork(galaxy, other, currentSystemId))
            {
                // A survey fleet already physically working the local target reserves it for
                // coordination purposes. This is reconstructible from live fleet + knowledge state.
                reserved.Add(currentSystemId);
            }
        }

        return reserved;
    }
}

public sealed record ExplorationAiMissionSelection(
    int FleetId,
    ExplorationMissionCandidate? Candidate,
    bool UsedSharedFallback,
    IReadOnlyList<int> ReservedSystemIds,
    string Reason)
{
    public static ExplorationAiMissionSelection None(int fleetId, string reason) =>
        new(fleetId, null, false, Array.Empty<int>(), reason);
}
