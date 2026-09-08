using System;
using System.Linq;
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
}

public sealed record MissionReachAssessment(
    bool IsSupported,
    bool IsAuthoritative,
    string Reason)
{
    public static MissionReachAssessment Supported(string reason = "Mission is within current operational reach.") =>
        new(true, true, reason);

    public static MissionReachAssessment Unsupported(string reason) =>
        new(false, true, string.IsNullOrWhiteSpace(reason) ? "Mission is beyond current operational reach." : reason);

    public static MissionReachAssessment ProvisionalSupported(string reason) =>
        new(true, false, reason);
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
