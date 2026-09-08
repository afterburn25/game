using System;
using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Transient command-side composition for one authoritative CombatSimulation plus its read-only
/// single/batch order previews. All three are constructed from the exact same hostility view so
/// campaign Core cannot accidentally preview with different political permission from issuance.
/// </summary>
public sealed class CombatCommandRuntime
{
    private readonly CombatCommandBatchService _batchCommands;

    public CombatCommandRuntime(ICombatHostilityView? hostilityView = null)
    {
        var sharedHostility = hostilityView ?? PeacefulCombatHostilityView.Instance;
        Simulation = new CombatSimulation(sharedHostility);
        OrderPreview = new CombatOrderPreviewService(sharedHostility);
        BatchOrderPreview = new CombatCommandBatchPreviewService(OrderPreview);
        _batchCommands = new CombatCommandBatchService(Simulation);
    }

    public CombatSimulation Simulation { get; }
    public CombatOrderPreviewService OrderPreview { get; }
    public CombatCommandBatchPreviewService BatchOrderPreview { get; }

    public CombatOrderResult IssueOrder(
        GalaxyState galaxy,
        int civilizationId,
        int fleetId,
        MilitaryOrder order) =>
        Simulation.IssueOrder(galaxy, civilizationId, fleetId, order);

    public CombatBatchOrderResult IssueOrders(
        GalaxyState galaxy,
        int civilizationId,
        IEnumerable<int> fleetIds,
        MilitaryOrder order) =>
        _batchCommands.IssueOrder(galaxy, civilizationId, fleetIds, order);

    public CombatOrderPreview PreviewOrder(
        GalaxyState galaxy,
        int civilizationId,
        int fleetId,
        MilitaryOrder order) =>
        OrderPreview.Preview(galaxy, civilizationId, fleetId, order);

    public CombatBatchOrderPreview PreviewOrders(
        GalaxyState galaxy,
        int civilizationId,
        IEnumerable<int> fleetIds,
        MilitaryOrder order) =>
        BatchOrderPreview.Preview(galaxy, civilizationId, fleetIds, order);
}
