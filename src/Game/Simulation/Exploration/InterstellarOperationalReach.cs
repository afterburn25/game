using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

/// <summary>
/// Consumer-side contract for practical interstellar mission reach. The authoritative
/// implementation belongs to Economy/Logistics; Exploration and Colonization only ask it
/// whether a physical fleet can support a mission to a target system.
/// </summary>
public interface IInterstellarOperationalReachView
{
    MissionReachAssessment Assess(
        GalaxyState galaxy,
        int civilizationId,
        FleetState fleet,
        int targetSystemId,
        InterstellarMissionKind missionKind);
}

public enum InterstellarMissionKind
{
    ScoutReconnaissance,
    ScienceSurvey,
    Colony,
    MilitaryDeployment,
}

public sealed record MissionReachAssessment(
    bool IsSupported,
    bool IsAuthoritative,
    string Reason,
    IReadOnlyList<int>? RouteSystemIds = null,
    double RouteDistanceLightYears = 0.0)
{
    public static MissionReachAssessment Supported(string reason = "Mission is within current operational reach.") =>
        new(true, true, reason);

    public static MissionReachAssessment Unsupported(string reason) =>
        new(false, true, string.IsNullOrWhiteSpace(reason) ? "Mission is beyond current operational reach." : reason);

    public static MissionReachAssessment ProvisionalSupported(string reason) =>
        new(true, false, reason);
}

/// <summary>Authoritative early-game reach over the generated interstellar lane graph.</summary>
public sealed class LaneInterstellarOperationalReachView : IInterstellarOperationalReachView
{
    private readonly InterstellarLaneNetwork _lanes = new();

    public MissionReachAssessment Assess(
        GalaxyState galaxy,
        int civilizationId,
        FleetState fleet,
        int targetSystemId,
        InterstellarMissionKind missionKind)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);
        if (fleet.CivilizationId != civilizationId)
            return MissionReachAssessment.Unsupported("That fleet is not controlled by this civilization.");
        if (!galaxy.Systems.Any(system => system.Id == targetSystemId))
            return MissionReachAssessment.Unsupported("Unknown mission target.");
        if (fleet.CurrentSystemId is not int originSystemId)
            return MissionReachAssessment.Unsupported("The fleet must finish its current lane leg before receiving a new interstellar route.");

        var route = _lanes.FindShortestRoute(
            galaxy.Systems,
            originSystemId,
            targetSystemId,
            fleet.MaximumLegRangeLightYears);
        if (route.Count == 0)
        {
            return MissionReachAssessment.Unsupported(
                $"No connected lane route is available within this fleet's {fleet.MaximumLegRangeLightYears:0.#} ly maximum leg range.");
        }

        var systems = galaxy.Systems.ToDictionary(system => system.Id);
        var distance = route.Zip(route.Skip(1), (first, second) =>
            (double)Vector2.Distance(systems[first].Position, systems[second].Position)).Sum();
        var legs = Math.Max(0, route.Count - 1);
        return new MissionReachAssessment(
            true,
            true,
            legs == 0
                ? "The fleet is already in the target system."
                : $"Route: {legs} lane leg{(legs == 1 ? string.Empty : "s")}, {distance:0.#} ly total; maximum leg {fleet.MaximumLegRangeLightYears:0.#} ly.",
            route,
            distance);
    }
}

public static class FleetRouteOrders
{
    public static void Assign(
        GalaxyState galaxy,
        FleetState fleet,
        int finalDestinationSystemId,
        MissionReachAssessment reach)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);
        ArgumentNullException.ThrowIfNull(reach);
        if (!reach.IsSupported)
            throw new InvalidOperationException("A fleet route can only be assigned from a supported reach assessment.");

        var route = reach.RouteSystemIds;
        if (route is null && fleet.CurrentSystemId is int originSystemId)
        {
            route = new InterstellarLaneNetwork().FindShortestRoute(
                galaxy.Systems,
                originSystemId,
                finalDestinationSystemId,
                fleet.MaximumLegRangeLightYears);
        }
        if (route is null || route.Count == 0)
            route = new[] { finalDestinationSystemId };

        fleet.DestinationSystemId = finalDestinationSystemId;
        fleet.PlannedRouteSystemIds = route
            .Where(systemId => systemId != fleet.CurrentSystemId)
            .ToList();
    }

    public static void Clear(FleetState fleet)
    {
        fleet.DestinationSystemId = null;
        fleet.PlannedRouteSystemIds.Clear();
    }
}

/// <summary>
/// Temporary compatibility adapter while Solar Economy/Logistics does not yet expose fleet
/// endurance / origin-to-target support. It deliberately contains no distance, fuel, supply,
/// or endurance formula. Once the authoritative logistics adapter is available, Core should
/// inject it into ExplorationSimulation and ColonizationSimulation.
/// </summary>
public sealed class PrototypeInterstellarOperationalReachView : IInterstellarOperationalReachView
{
    public MissionReachAssessment Assess(
        GalaxyState galaxy,
        int civilizationId,
        FleetState fleet,
        int targetSystemId,
        InterstellarMissionKind missionKind)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);
        if (!galaxy.Systems.Any(system => system.Id == targetSystemId))
            return MissionReachAssessment.Unsupported("Unknown mission target.");

        return MissionReachAssessment.ProvisionalSupported(
            "Operational reach is provisionally available until the authoritative logistics endurance contract is integrated.");
    }
}
