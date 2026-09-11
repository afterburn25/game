using System;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

/// <summary>
/// Bounded, normalized system-chart transit. A chart unit is a game presentation unit, not an
/// orbital distance: these legs consume time and operating funding but never light-year fuel.
/// </summary>
public static class FleetLocalTransit
{
    public const float GateRadius = .82f;
    private const double BaseChartUnitsPerDay = .9;
    private const double ReferenceStrategicSpeed = 22.0;

    public static double Rate(FleetState fleet) => Math.Max(.1,
        BaseChartUnitsPerDay * Math.Max(.1, fleet.StrategicSpeed) / ReferenceStrategicSpeed);

    public static Vector2 GateTowards(Vector2 sourceSystemPosition, Vector2 currentSystemPosition)
    {
        var direction = sourceSystemPosition - currentSystemPosition;
        return direction.LengthSquared() <= .000001f
            ? Vector2.UnitX * GateRadius
            : Vector2.Normalize(direction) * GateRadius;
    }

    public static void Begin(FleetState fleet, FleetTransitPhase phase, Vector2 start, Vector2 target)
    {
        fleet.TransitPhase = phase;
        fleet.LocalTransitStart = Finite(start) ? start : Vector2.Zero;
        fleet.LocalTransitPosition = fleet.LocalTransitStart;
        fleet.LocalTransitTarget = Finite(target) ? target : Vector2.Zero;
        fleet.TransitProgress = 0;
    }

    public static double Advance(FleetState fleet, double availableDays)
    {
        if (availableDays <= 0 || !double.IsFinite(availableDays)) return 0;
        var start = Finite(fleet.LocalTransitStart) ? fleet.LocalTransitStart : Vector2.Zero;
        var position = Finite(fleet.LocalTransitPosition) ? fleet.LocalTransitPosition : start;
        var target = Finite(fleet.LocalTransitTarget) ? fleet.LocalTransitTarget : Vector2.Zero;
        var remaining = Vector2.Distance(position, target);
        if (remaining <= .00001f)
        {
            fleet.LocalTransitPosition = target;
            fleet.TransitProgress = 1;
            return 0;
        }
        var requiredDays = remaining / Rate(fleet);
        var spent = Math.Min(availableDays, requiredDays);
        fleet.LocalTransitPosition = Vector2.Lerp(position, target, (float)(spent / requiredDays));
        var total = Vector2.Distance(start, target);
        fleet.TransitProgress = total <= .00001f ? 1 : Math.Clamp(Vector2.Distance(start, fleet.LocalTransitPosition) / total, 0, 1);
        return spent;
    }

    public static bool Complete(FleetState fleet) =>
        Vector2.DistanceSquared(fleet.LocalTransitPosition, fleet.LocalTransitTarget) <= .00000001f;

    public static double RemainingDays(FleetState fleet) =>
        fleet.TransitPhase is FleetTransitPhase.LocalDeparture or FleetTransitPhase.LocalArrival
            ? Vector2.Distance(fleet.LocalTransitPosition, fleet.LocalTransitTarget) / Rate(fleet)
            : 0;

    public static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
