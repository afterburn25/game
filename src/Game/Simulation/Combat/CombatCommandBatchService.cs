using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Applies one military order to a transient multi-selection of physical fleets.
/// This is a command surface, not a persistent fleet/task-force membership model.
/// Results are per fleet so heterogeneous selections can partially succeed without
/// silently dropping rejected vessels or rolling back valid orders.
/// </summary>
public sealed class CombatCommandBatchService
{
    private readonly CombatSimulation _combat;

    public CombatCommandBatchService(CombatSimulation combat)
    {
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
    }

    public CombatBatchOrderResult IssueOrder(
        GalaxyState galaxy,
        int civilizationId,
        IEnumerable<int> fleetIds,
        MilitaryOrder order)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleetIds);
        ArgumentNullException.ThrowIfNull(order);

        var uniqueFleetIds = fleetIds
            .Distinct()
            .OrderBy(fleetId => fleetId)
            .ToArray();
        var results = new List<FleetCombatOrderResult>(uniqueFleetIds.Length);

        foreach (var fleetId in uniqueFleetIds)
        {
            var result = _combat.IssueOrder(galaxy, civilizationId, fleetId, order);
            results.Add(new FleetCombatOrderResult(fleetId, result.Accepted, result.Message));
        }

        return new CombatBatchOrderResult(
            uniqueFleetIds.Length,
            results.Count(result => result.Accepted),
            results.Count(result => !result.Accepted),
            results);
    }
}

public sealed record FleetCombatOrderResult(
    int FleetId,
    bool Accepted,
    string Message);

public sealed record CombatBatchOrderResult(
    int RequestedFleetCount,
    int AcceptedCount,
    int RejectedCount,
    IReadOnlyList<FleetCombatOrderResult> FleetResults)
{
    public bool AllAccepted => RequestedFleetCount > 0 && RejectedCount == 0;
    public bool AnyAccepted => AcceptedCount > 0;
}
