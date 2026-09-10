using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public sealed class ExplorationSimulation
{
    // Retained as the original/reference survey rate for callers that display historical tuning.
    // Active science progress now comes from the bounded SurveyOperationsProfile per system.
    public const double ScienceSurveyProgressPerDay = 0.08;
    public const double ScoutReconnaissanceProgress = 0.35;

    private readonly SurveyOperationsProfiler _surveyProfiler;
    private readonly ExplorationMissionPlanner _missionPlanner;
    private readonly ExplorationAiMissionCoordinator _aiMissionCoordinator;

    public ExplorationSimulation(
        IInterstellarOperationalReachView? operationalReach = null,
        SurveyOperationsProfiler? surveyProfiler = null)
    {
        var reach = operationalReach ?? new LaneInterstellarOperationalReachView();
        _surveyProfiler = surveyProfiler ?? new SurveyOperationsProfiler();
        _missionPlanner = new ExplorationMissionPlanner(reach, _surveyProfiler);
        _aiMissionCoordinator = new ExplorationAiMissionCoordinator(_missionPlanner);
    }

    public IReadOnlyList<ExplorationEvent> Advance(GalaxyState galaxy, double simulationDelta)
    {
        if (simulationDelta <= 0.0)
            return Array.Empty<ExplorationEvent>();

        var events = new List<ExplorationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);
            if (fleet.CurrentSystemId is int refuelSystemId && galaxy.Colonies.Any(colony =>
                    colony.CivilizationId == fleet.CivilizationId && colony.SystemId == refuelSystemId))
                fleet.FuelRemainingLightYears = fleet.FuelCapacityLightYears;

            if (fleet.DestinationSystemId is null &&
                IsSurveyFleet(fleet) &&
                fleet.CurrentSystemId is int localSystemId &&
                ProcessLocalSurvey(galaxy, fleet, localSystemId, simulationDelta, events))
            {
                // An actively surveying vessel is a legitimate directional observer of foreign
                // presence in its current system. This keeps first-contact semantics one-way:
                // passive foreign fleets/colonies do not automatically receive reciprocal
                // knowledge merely because another civilization is surveying nearby.
                DetectCivilizationContacts(galaxy, fleet, events);

                // Surveying consumes this fleet's activity for the current simulation step.
                // This avoids frame/order-dependent survey + movement in the same tick.
                continue;
            }

            if (fleet.DestinationSystemId is null && !civilization.IsPlayer && IsSurveyFleet(fleet))
                AssignAiSurveyDestination(galaxy, fleet);

            if (fleet.DestinationSystemId is null)
                continue;

            var remainingStep = fleet.StrategicSpeed * simulationDelta;
            while (fleet.DestinationSystemId is not null && remainingStep > 0.0)
            {
                var movementTargetId = fleet.PlannedRouteSystemIds.Count > 0
                    ? fleet.PlannedRouteSystemIds[0]
                    : fleet.DestinationSystemId.Value;
                var target = galaxy.Systems.First(s => s.Id == movementTargetId);
                var toTarget = target.Position - fleet.Position;
                var distance = toTarget.Length();

                var availableStep = Math.Min(remainingStep, fleet.FuelRemainingLightYears);
                if (distance <= availableStep || distance <= 0.001f)
                {
                    remainingStep = Math.Max(0.0, remainingStep - distance);
                    fleet.FuelRemainingLightYears = Math.Max(0.0, fleet.FuelRemainingLightYears - distance);
                    fleet.Position = target.Position;
                    fleet.CurrentSystemId = target.Id;
                    if (fleet.PlannedRouteSystemIds.Count > 0)
                        fleet.PlannedRouteSystemIds.RemoveAt(0);
                    var reachedFinalDestination = target.Id == fleet.DestinationSystemId && fleet.PlannedRouteSystemIds.Count == 0;
                    if (reachedFinalDestination)
                        fleet.DestinationSystemId = null;
                    if (galaxy.Colonies.Any(colony =>
                            colony.CivilizationId == fleet.CivilizationId && colony.SystemId == target.Id))
                    {
                        fleet.FuelRemainingLightYears = fleet.FuelCapacityLightYears;
                    }

                    var alreadyKnown = galaxy.Knowledge.IsSystemKnown(fleet.CivilizationId, target.Id);
                    var revealed = galaxy.Knowledge.RevealWithinSensorRange(
                        fleet.CivilizationId,
                        target.Id,
                        galaxy.Systems,
                        fleet.SensorRange);

                    if (!alreadyKnown)
                    {
                        events.Add(new ExplorationEvent(
                            ExplorationEventType.SystemDetected,
                            fleet.CivilizationId,
                            fleet.Id,
                            target.Id,
                            $"{fleet.Name} reached astronomical target {target.Id + 1:000}; detailed system data still requires survey work."));
                    }

                    if (revealed > 0)
                    {
                        events.Add(new ExplorationEvent(
                            ExplorationEventType.SensorContact,
                            fleet.CivilizationId,
                            fleet.Id,
                            target.Id,
                            $"Sensors added {revealed} system{(revealed == 1 ? string.Empty : "s")} to the local chart."));
                    }

                    DetectCivilizationContacts(galaxy, fleet, events);
                    continue;
                }

                if (availableStep <= 0.0)
                    break;
                var direction = Vector2.Normalize(toTarget);
                fleet.Position += direction * (float)availableStep;
                fleet.FuelRemainingLightYears = Math.Max(0.0, fleet.FuelRemainingLightYears - availableStep);
                fleet.CurrentSystemId = null;
                remainingStep = 0.0;
            }
        }

        return events;
    }

    /// <summary>
    /// Compatibility wrapper retained for existing callers. New UI should use IssueSurveyOrder
    /// so a rejected order carries the same reason and candidate state used by AI planning.
    /// </summary>
    public bool IssueMoveOrder(GalaxyState galaxy, int fleetId, int destinationSystemId) =>
        IssueSurveyOrder(galaxy, fleetId, destinationSystemId).Accepted;

    public ExplorationMissionOrderAssessment IssueSurveyOrder(
        GalaxyState galaxy,
        int fleetId,
        int destinationSystemId)
    {
        var assessment = _missionPlanner.AssessOrder(galaxy, fleetId, destinationSystemId);
        if (!assessment.Accepted)
            return assessment;

        var fleet = galaxy.Fleets.First(f => f.Id == fleetId && f.IsActive);
        if (assessment.IsLocalSurvey)
        {
            fleet.DestinationSystemId = null;
            return assessment;
        }

        FleetRouteOrders.Assign(galaxy, fleet, destinationSystemId, assessment.Candidate!.Reach);
        return assessment;
    }

    public ExplorationMissionPlan GetMissionPlan(
        GalaxyState galaxy,
        int fleetId,
        int maximumCandidates = ExplorationMissionPlanner.DefaultMaximumCandidates) =>
        _missionPlanner.BuildPlan(galaxy, fleetId, maximumCandidates);

    public MissionReachAssessment AssessOperationalReach(GalaxyState galaxy, int fleetId, int destinationSystemId)
    {
        var fleet = galaxy.Fleets.FirstOrDefault(f => f.Id == fleetId && f.IsActive);
        return fleet is null
            ? MissionReachAssessment.Unsupported("No active exploration vessel is available.")
            : _missionPlanner.AssessOperationalReach(galaxy, fleet, destinationSystemId);
    }

    public SurveyOperationsProfile GetSurveyOperationsProfile(GalaxyState galaxy, int systemId) =>
        _surveyProfiler.Build(galaxy, systemId);

    private static bool IsSurveyFleet(FleetState fleet) => fleet.Role is FleetRole.Scout or FleetRole.Science;

    private bool ProcessLocalSurvey(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId,
        double simulationDelta,
        ICollection<ExplorationEvent> events)
    {
        if (fleet.Role == FleetRole.Scout)
        {
            if (galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId) >= SystemSurveyLevel.PartiallySurveyed)
                return false;

            if (!galaxy.Knowledge.RecordReconnaissance(
                    fleet.CivilizationId,
                    systemId,
                    ScoutReconnaissanceProgress))
            {
                return false;
            }

            var system = galaxy.Systems.First(s => s.Id == systemId);
            var profile = _surveyProfiler.Build(galaxy, systemId);
            events.Add(new ExplorationEvent(
                ExplorationEventType.SystemReconnoitered,
                fleet.CivilizationId,
                fleet.Id,
                systemId,
                $"{fleet.Name} completed a rapid reconnaissance pass of {system.Name}; estimated detailed survey effort is {profile.EstimatedScienceSurveyDays:0.#} days ({profile.OperationalHazard.ToString().ToLowerInvariant()} survey conditions)."));
            EmitReconnaissanceSignatures(galaxy, fleet, systemId, events);
            return true;
        }

        if (fleet.Role != FleetRole.Science || galaxy.Knowledge.IsSystemFullySurveyed(fleet.CivilizationId, systemId))
            return false;

        var previousLevel = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        var previousProgress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId);
        var profileForSystem = _surveyProfiler.Build(galaxy, systemId);
        var completed = galaxy.Knowledge.AdvanceSystemSurvey(
            fleet.CivilizationId,
            systemId,
            profileForSystem.ProgressPerDay * simulationDelta);
        var currentProgress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId);
        var currentLevel = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);

        if (currentProgress <= previousProgress + 0.0000001)
            return false;

        var systemState = galaxy.Systems.First(s => s.Id == systemId);
        if (previousLevel < SystemSurveyLevel.PartiallySurveyed)
        {
            events.Add(new ExplorationEvent(
                ExplorationEventType.SystemSurveyStarted,
                fleet.CivilizationId,
                fleet.Id,
                systemId,
                $"{fleet.Name} began a detailed science survey of {systemState.Name}; estimated total effort is {profileForSystem.EstimatedScienceSurveyDays:0.#} days."));

            // A science vessel can establish reconnaissance-grade knowledge without a scout.
            // Emit the same positive-only signature evidence exactly on that transition so
            // event consumers stay synchronized with the observer-safe read model. If a very
            // large deterministic step jumps directly to a full survey, skip transient
            // unconfirmed signatures and emit only confirmed discoveries below.
            if (currentLevel == SystemSurveyLevel.PartiallySurveyed)
                EmitReconnaissanceSignatures(galaxy, fleet, systemId, events);
        }

        if (completed)
        {
            events.Add(new ExplorationEvent(
                ExplorationEventType.SystemSurveyed,
                fleet.CivilizationId,
                fleet.Id,
                systemId,
                $"{fleet.Name} completed a detailed survey of {systemState.Name}."));
            EmitConfirmedBodyDiscoveries(galaxy, fleet, systemId, events);
        }

        return true;
    }

    private static void EmitReconnaissanceSignatures(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId,
        ICollection<ExplorationEvent> events)
    {
        foreach (var body in galaxy.PlanetaryBodies.Where(body => body.SystemId == systemId).OrderBy(body => body.Id))
        {
            if (body.HasRareResource)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.ResourceSignatureDetected,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} detected an unusual resource signature near {body.Name}; detailed survey is required to confirm it.",
                    PlanetaryBodyId: body.Id));
            }
            if (body.HasAnomaly)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.AnomalySignatureDetected,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} detected an anomalous signature near {body.Name}; its nature remains unconfirmed.",
                    PlanetaryBodyId: body.Id));
            }
            if (body.HasPreWarpCivilization)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.ActivitySignatureDetected,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} detected unresolved activity signatures from {body.Name}; detailed survey is required before classification.",
                    PlanetaryBodyId: body.Id));
            }
        }
    }

    private static void EmitConfirmedBodyDiscoveries(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId,
        ICollection<ExplorationEvent> events)
    {
        foreach (var body in galaxy.PlanetaryBodies.Where(body => body.SystemId == systemId).OrderBy(body => body.Id))
        {
            if (body.HasAnomaly)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.AnomalySurveyed,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} confirmed an anomaly on or near {body.Name}.",
                    PlanetaryBodyId: body.Id));
            }
            if (body.HasRareResource)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.ResourceSurveyed,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} confirmed a rare-resource deposit or signature associated with {body.Name}.",
                    PlanetaryBodyId: body.Id));
            }
            if (body.HasPreWarpCivilization)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.NativeCivilizationSurveyed,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} confirmed a native pre-warp civilization on {body.Name}.",
                    PlanetaryBodyId: body.Id));
            }
        }
    }

    private void AssignAiSurveyDestination(GalaxyState galaxy, FleetState fleet)
    {
        var selection = _aiMissionCoordinator.SelectMission(galaxy, fleet);
        if (selection.Candidate is not null)
            FleetRouteOrders.Assign(galaxy, fleet, selection.Candidate.SystemId, selection.Candidate.Reach);
    }

    private static void DetectCivilizationContacts(GalaxyState galaxy, FleetState fleet, ICollection<ExplorationEvent> events)
    {
        if (fleet.CurrentSystemId is not int currentSystemId)
            return;

        foreach (var other in galaxy.Civilizations)
        {
            if (other.Id == fleet.CivilizationId ||
                galaxy.Knowledge.IsCivilizationKnown(fleet.CivilizationId, other.Id))
            {
                continue;
            }

            // A catalog/sensor-known star is not evidence of who lives there. Identification
            // requires actual same-system foreign presence until Diplomacy provides a richer
            // legitimate-contact opportunity model (signals, hails, remote detection, etc.).
            var foreignColonyPresent = galaxy.Colonies.Any(colony =>
                colony.CivilizationId == other.Id && colony.SystemId == currentSystemId);
            var foreignFleetPresent = galaxy.Fleets.Any(otherFleet =>
                otherFleet.IsActive &&
                otherFleet.CivilizationId == other.Id &&
                otherFleet.CurrentSystemId == currentSystemId);
            if (!foreignColonyPresent && !foreignFleetPresent)
                continue;

            galaxy.Knowledge.RevealCivilization(fleet.CivilizationId, other.Id);
            events.Add(new ExplorationEvent(
                ExplorationEventType.FirstContact,
                fleet.CivilizationId,
                fleet.Id,
                currentSystemId,
                $"First contact: {other.Name}.",
                other.Id));
        }
    }
}

public enum ExplorationEventType
{
    SystemDetected,
    SystemReconnoitered,
    SystemSurveyStarted,
    SystemSurveyed,
    ResourceSignatureDetected,
    AnomalySignatureDetected,
    ActivitySignatureDetected,
    ResourceSurveyed,
    AnomalySurveyed,
    NativeCivilizationSurveyed,
    SensorContact,
    FirstContact,
}

public sealed record ExplorationEvent(
    ExplorationEventType Type,
    int CivilizationId,
    int FleetId,
    int SystemId,
    string Message,
    int? TargetCivilizationId = null,
    int? PlanetaryBodyId = null);
