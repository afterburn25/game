using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.AI;

/// <summary>
/// High-level strategic guidance that subsystem-owned selectors can consume without handing
/// their mechanics to Civilization AI. Weights are comparative priorities, not permission
/// gates and not hidden bonuses.
/// </summary>
public sealed record CivilizationStrategicIntent(
    int CivilizationId,
    long GeneratedAtTick,
    long ReviewAfterTick,
    IReadOnlyDictionary<StrategicPriorityType, double> Weights,
    FleetRole? PreferredNewFleetRole,
    bool DeferNewColonization,
    string Summary
)
{
    public double GetWeight(StrategicPriorityType type) =>
        Weights.TryGetValue(type, out var weight) ? weight : 0.0;
}

public sealed class CivilizationStrategicIntentBuilder
{
    public CivilizationStrategicIntent Build(CivilizationStrategicPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var weights = Enum.GetValues<StrategicPriorityType>()
            .ToDictionary(type => type, _ => 0.0);

        foreach (var priority in plan.Priorities)
            weights[priority.Type] = Math.Clamp(priority.Score, 0.0, 2.0);

        var supplyWeight = weights[StrategicPriorityType.StabilizeSupply];
        var defenseWeight = weights[StrategicPriorityType.Defend];
        var colonizationWeight = weights[StrategicPriorityType.Colonize];
        var explorationWeight = weights[StrategicPriorityType.Explore];
        var fleetWeight = weights[StrategicPriorityType.BuildFleet];

        // Expansion is restrained by actual strategic pressure rather than an arbitrary empire cap.
        var deferColonization = supplyWeight > Math.Max(0.75, colonizationWeight)
                                || defenseWeight > colonizationWeight + 0.35;

        FleetRole? preferredFleetRole = null;
        if (defenseWeight >= Math.Max(explorationWeight, colonizationWeight) && defenseWeight >= 0.60)
            preferredFleetRole = FleetRole.Military;
        else if (colonizationWeight >= explorationWeight && colonizationWeight >= 0.55 && !deferColonization)
            preferredFleetRole = FleetRole.Colony;
        else if (explorationWeight >= 0.45)
            preferredFleetRole = FleetRole.Scout;
        else if (fleetWeight >= 0.55)
            preferredFleetRole = FleetRole.Military;

        var primary = plan.PrimaryPriority;
        var summary = primary is null
            ? "No urgent strategic priority."
            : $"Primary: {primary.Type} ({primary.Score:0.00}) — {primary.Reason}";

        return new CivilizationStrategicIntent(
            plan.CivilizationId,
            plan.GeneratedAtTick,
            plan.ReviewAfterTick,
            weights,
            preferredFleetRole,
            deferColonization,
            summary);
    }
}
