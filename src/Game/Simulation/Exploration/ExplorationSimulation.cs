using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public sealed class ExplorationSimulation
{
    public IReadOnlyList<ExplorationEvent> Advance(GalaxyState galaxy, double simulationDelta)
    {
        if (simulationDelta <= 0.0)
            return Array.Empty<ExplorationEvent>();

        var events = new List<ExplorationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);

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
                    var surveyVerb = fleet.Role == FleetRole.Science ? "completed a deep survey of" : "surveyed";
                    events.Add(new ExplorationEvent(ExplorationEventType.SystemSurveyed, fleet.CivilizationId, fleet.Id, target.Id, $"{fleet.Name} {surveyVerb} {target.Name}."));

                    if (fleet.Role == FleetRole.Science && target.HasAnomaly)
                        events.Add(new ExplorationEvent(ExplorationEventType.AnomalySurveyed, fleet.CivilizationId, fleet.Id, target.Id, $"{fleet.Name} identified an anomaly during its deep survey of {target.Name}."));
                }

                if (revealed > 0)
                    events.Add(new ExplorationEvent(ExplorationEventType.SensorContact, fleet.CivilizationId, fleet.Id, target.Id, $"Sensors added {revealed} system{(revealed == 1 ? string.Empty : "s")} to the local chart."));

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

    private static void AssignAiSurveyDestination(GalaxyState galaxy, FleetState fleet)
    {
        var unknown = galaxy.Systems
            .Where(system => !galaxy.Knowledge.IsSystemKnown(fleet.CivilizationId, system.Id))
            .OrderBy(system => Vector2.DistanceSquared(fleet.Position, system.Position))
            .FirstOrDefault();
        if (unknown is not null)
            fleet.DestinationSystemId = unknown.Id;
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
            events.Add(new ExplorationEvent(ExplorationEventType.FirstContact, fleet.CivilizationId, fleet.Id, other.HomeSystemId, $"First contact: {other.Name}."));
        }
    }
}

public enum ExplorationEventType { SystemSurveyed, AnomalySurveyed, SensorContact, FirstContact }
public sealed record ExplorationEvent(ExplorationEventType Type, int CivilizationId, int FleetId, int SystemId, string Message);
