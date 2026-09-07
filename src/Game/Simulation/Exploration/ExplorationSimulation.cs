using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public sealed class ExplorationSimulation
{
    // Detailed science surveys are deliberately slower than a scout fly-through.
    // The input to Advance is simulated days, so the rate is deterministic and
    // independent from frame rate or presentation timing.
    public const double ScienceSurveyProgressPerDay = 0.08;
    public const double ScoutReconnaissanceProgress = 0.35;

    public IReadOnlyList<ExplorationEvent> Advance(GalaxyState galaxy, double simulationDelta)
    {
        if (simulationDelta <= 0.0)
            return Array.Empty<ExplorationEvent>();

        var events = new List<ExplorationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);

            if (fleet.DestinationSystemId is null &&
                IsSurveyFleet(fleet) &&
                fleet.CurrentSystemId is int localSystemId &&
                ProcessLocalSurvey(galaxy, fleet, localSystemId, simulationDelta, events))
            {
                // Surveying consumes this fleet's activity for the current simulation step.
                // This avoids frame/order-dependent survey + movement in the same tick.
                continue;
            }

            if (fleet.DestinationSystemId is null && !civilization.IsPlayer && IsSurveyFleet(fleet))
                AssignAiSurveyDestination(galaxy, fleet);

            if (fleet.DestinationSystemId is null)
                continue;

            var target = galaxy.Systems.First(s => s.Id == fleet.DestinationSystemId.Value);
            var toTarget = target.Position - fleet.Position;
            var distance = toTarget.Length();
            var step = fleet.StrategicSpeed * simulationDelta;

            if (distance <= step || distance <= 0.001f)
            {
                fleet.Position = target.Position;
                fleet.CurrentSystemId = target.Id;
                fleet.DestinationSystemId = null;

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
            }
            else
            {
                var direction = Vector2.Normalize(toTarget);
                fleet.Position += direction * (float)step;
                fleet.CurrentSystemId = null;
            }
        }

        return events;
    }

    public bool IssueMoveOrder(GalaxyState galaxy, int fleetId, int destinationSystemId)
    {
        var fleet = galaxy.Fleets.FirstOrDefault(f => f.Id == fleetId && f.IsActive);
        if (fleet is null || !galaxy.Systems.Any(s => s.Id == destinationSystemId))
            return false;

        fleet.DestinationSystemId = destinationSystemId;
        return true;
    }

    private static bool IsSurveyFleet(FleetState fleet) => fleet.Role is FleetRole.Scout or FleetRole.Science;

    private static bool ProcessLocalSurvey(
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
            events.Add(new ExplorationEvent(
                ExplorationEventType.SystemReconnoitered,
                fleet.CivilizationId,
                fleet.Id,
                systemId,
                $"{fleet.Name} completed a rapid reconnaissance pass of {system.Name}; a science survey is still required for colonization-grade data."));
            return true;
        }

        if (fleet.Role != FleetRole.Science || galaxy.Knowledge.IsSystemFullySurveyed(fleet.CivilizationId, systemId))
            return false;

        var previousLevel = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        var previousProgress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId);
        var completed = galaxy.Knowledge.AdvanceSystemSurvey(
            fleet.CivilizationId,
            systemId,
            ScienceSurveyProgressPerDay * simulationDelta);
        var currentProgress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId);

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
                $"{fleet.Name} began a detailed science survey of {systemState.Name}."));
        }

        if (completed)
        {
            events.Add(new ExplorationEvent(
                ExplorationEventType.SystemSurveyed,
                fleet.CivilizationId,
                fleet.Id,
                systemId,
                $"{fleet.Name} completed a detailed survey of {systemState.Name}."));

            if (systemState.HasAnomaly)
            {
                events.Add(new ExplorationEvent(
                    ExplorationEventType.AnomalySurveyed,
                    fleet.CivilizationId,
                    fleet.Id,
                    systemId,
                    $"{fleet.Name} identified an anomaly during its detailed survey of {systemState.Name}."));
            }
        }

        return true;
    }

    private static void AssignAiSurveyDestination(GalaxyState galaxy, FleetState fleet)
    {
        var candidate = galaxy.Systems
            .Where(system => NeedsSurveyWork(galaxy, fleet, system.Id))
            .Select(system => new
            {
                System = system,
                Priority = SurveyPriority(galaxy, fleet, system.Id),
                Distance = Vector2.DistanceSquared(fleet.Position, system.Position),
            })
            .OrderBy(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.System.Id)
            .FirstOrDefault();

        if (candidate is not null)
            fleet.DestinationSystemId = candidate.System.Id;
    }

    private static bool NeedsSurveyWork(GalaxyState galaxy, FleetState fleet, int systemId)
    {
        var level = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        return fleet.Role switch
        {
            FleetRole.Scout => level < SystemSurveyLevel.PartiallySurveyed,
            FleetRole.Science => level < SystemSurveyLevel.FullySurveyed,
            _ => false,
        };
    }

    private static int SurveyPriority(GalaxyState galaxy, FleetState fleet, int systemId)
    {
        var level = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        if (fleet.Role == FleetRole.Science)
        {
            // Science ships preferentially finish information already gathered by scouts/sensors
            // before striking out toward merely catalogued astronomical targets.
            return level switch
            {
                SystemSurveyLevel.PartiallySurveyed => 0,
                SystemSurveyLevel.Detected => 1,
                _ => 2,
            };
        }

        // Scouts first extend an existing detected frontier, then move into catalogued unknowns.
        return level == SystemSurveyLevel.Detected ? 0 : 1;
    }

    private static void DetectCivilizationContacts(GalaxyState galaxy, FleetState fleet, ICollection<ExplorationEvent> events)
    {
        foreach (var other in galaxy.Civilizations)
        {
            if (other.Id == fleet.CivilizationId ||
                !galaxy.Knowledge.IsSystemKnown(fleet.CivilizationId, other.HomeSystemId) ||
                galaxy.Knowledge.IsCivilizationKnown(fleet.CivilizationId, other.Id))
                continue;

            galaxy.Knowledge.RevealCivilization(fleet.CivilizationId, other.Id);
            events.Add(new ExplorationEvent(
                ExplorationEventType.FirstContact,
                fleet.CivilizationId,
                fleet.Id,
                other.HomeSystemId,
                $"First contact: {other.Name}."));
        }
    }
}

public enum ExplorationEventType
{
    SystemDetected,
    SystemReconnoitered,
    SystemSurveyStarted,
    SystemSurveyed,
    AnomalySurveyed,
    SensorContact,
    FirstContact,
}

public sealed record ExplorationEvent(
    ExplorationEventType Type,
    int CivilizationId,
    int FleetId,
    int SystemId,
    string Message);
