using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.AI;
using Game.Simulation.Colonization;
using Game.Simulation.Combat;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Exploration;
using Game.Simulation.Industry;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation;

/// <summary>
/// One authoritative strategic simulation step. Godot supplies only accepted simulation time;
/// subsystem mutation order and shared Industry allocation are resolved here in plain C#.
/// </summary>
public sealed class GalaxySimulationStepCoordinator
{
    private readonly EconomySimulation _economy;
    private readonly ConstructionSimulation _construction;
    private readonly ShipbuildingSimulation _shipbuilding;
    private readonly ResearchSimulation _research;
    private readonly ExplorationSimulation _exploration;
    private readonly CombatSimulation _combat;
    private readonly ColonizationSimulation _colonization;
    private readonly CivilizationStrategicRuntimeCoordinator _strategicAi;
    private readonly IIndustryAllocationPolicy _industryAllocationPolicy;

    public GalaxySimulationStepCoordinator(
        EconomySimulation? economy = null,
        ConstructionSimulation? construction = null,
        ShipbuildingSimulation? shipbuilding = null,
        ResearchSimulation? research = null,
        ExplorationSimulation? exploration = null,
        ColonizationSimulation? colonization = null,
        IIndustryAllocationPolicy? industryAllocationPolicy = null,
        CombatSimulation? combat = null,
        CivilizationStrategicRuntimeCoordinator? strategicAi = null)
    {
        _economy = economy ?? new EconomySimulation();
        _construction = construction ?? new ConstructionSimulation();
        _shipbuilding = shipbuilding ?? new ShipbuildingSimulation();
        _research = research ?? new ResearchSimulation();
        _exploration = exploration ?? new ExplorationSimulation();
        _combat = combat ?? new CombatSimulation();
        _colonization = colonization ?? new ColonizationSimulation();
        _strategicAi = strategicAi ?? new CivilizationStrategicRuntimeCoordinator();
        _industryAllocationPolicy = industryAllocationPolicy
            ?? new WeightedFairIndustryAllocationPolicy(_strategicAi.IndustryPriorityProvider);
    }

    public CombatOrderResult IssueMilitaryOrder(
        GalaxyState galaxy,
        int civilizationId,
        int fleetId,
        MilitaryOrder order) =>
        _combat.IssueOrder(galaxy, civilizationId, fleetId, order);

    public CombatBatchOrderResult IssueMilitaryOrders(
        GalaxyState galaxy,
        int civilizationId,
        IEnumerable<int> fleetIds,
        MilitaryOrder order) =>
        new CombatCommandBatchService(_combat).IssueOrder(galaxy, civilizationId, fleetIds, order);

    public MilitaryForceSummary GetOwnMilitaryForceSummary(GalaxyState galaxy, int civilizationId) =>
        _combat.GetOwnMilitaryForceSummary(galaxy, civilizationId);

    public SimulationStepResult Advance(GalaxyState galaxy, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");
        if (simulationDays <= 0.0)
            return SimulationStepResult.Empty;

        // Generation occurs before strategy/allocation so every consumer sees the same stockpile snapshot.
        _economy.Advance(galaxy, simulationDays);

        // Civilization strategy is derived, bounded and own-state-only until a persisted
        // Diplomacy/intelligence runtime exists. It publishes comparative Industry weights;
        // Core remains authoritative for demand, allocation and resource spending.
        _strategicAi.Advance(galaxy, simulationDays);

        // AI may create orders at the boundary of a step. Those orders then participate in the
        // same deterministic allocation pass as player-created orders.
        _construction.EnsureAutomaticOrders(galaxy);
        _shipbuilding.EnsureAutomaticOrders(galaxy);

        var constructionBudgets = new Dictionary<int, double>();
        var shipbuildingBudgets = new Dictionary<int, double>();
        var allocations = new List<CivilizationIndustryAllocation>();

        foreach (var civilization in galaxy.Civilizations.Where(civilization => !civilization.IsSeededAncient))
        {
            var economy = galaxy.Economies.First(state => state.CivilizationId == civilization.Id);
            var allocation = _industryAllocationPolicy.Allocate(new IndustryAllocationContext(
                civilization.Id,
                Math.Max(0.0, economy.Industry),
                _construction.GetIndustryDemand(galaxy, civilization.Id),
                _shipbuilding.GetIndustryDemand(galaxy, civilization.Id)));

            constructionBudgets[civilization.Id] = allocation.ConstructionAllocated;
            shipbuildingBudgets[civilization.Id] = allocation.ShipbuildingAllocated;
            allocations.Add(allocation);
        }

        // Industry budgets were resolved from one pre-spend snapshot, so these calls cannot
        // steal capacity from each other based on callback order.
        var constructionEvents = _construction.Advance(galaxy, constructionBudgets);
        var shipbuildingEvents = _shipbuilding.Advance(galaxy, shipbuildingBudgets);

        // Research and newly completed construction can affect eligibility only on a later
        // step. This produces a clean causal boundary instead of mid-step unlock ordering.
        var researchEvents = _research.Advance(galaxy);

        // Movement/encounter state resolves before combat. Combat then resolves before
        // colonization so a vessel destroyed in an engagement cannot found a colony later
        // in the same authoritative step.
        var explorationEvents = _exploration.Advance(galaxy, simulationDays);
        var combatEvents = _combat.Advance(galaxy, simulationDays);
        var colonizationEvents = _colonization.Advance(galaxy);

        return new SimulationStepResult(
            simulationDays,
            allocations,
            constructionEvents,
            shipbuildingEvents,
            researchEvents,
            explorationEvents,
            combatEvents,
            colonizationEvents);
    }
}

public sealed record SimulationStepResult(
    double SimulationDays,
    IReadOnlyList<CivilizationIndustryAllocation> IndustryAllocations,
    IReadOnlyList<ConstructionEvent> ConstructionEvents,
    IReadOnlyList<ShipbuildingEvent> ShipbuildingEvents,
    IReadOnlyList<ResearchEvent> ResearchEvents,
    IReadOnlyList<ExplorationEvent> ExplorationEvents,
    IReadOnlyList<CombatEvent> CombatEvents,
    IReadOnlyList<ColonizationEvent> ColonizationEvents)
{
    /// <summary>
    /// Compact authoritative aggregate derived only from this step's CombatEvents. Raw events
    /// remain available unchanged; this property adds no persistent state and should not be
    /// exposed directly to a fog-of-war observer without first filtering the underlying events.
    /// </summary>
    public CombatOutcomeSummary CombatOutcome => CombatOutcomeSummaryBuilder.Build(CombatEvents);

    public static SimulationStepResult Empty { get; } = new(
        0.0,
        Array.Empty<CivilizationIndustryAllocation>(),
        Array.Empty<ConstructionEvent>(),
        Array.Empty<ShipbuildingEvent>(),
        Array.Empty<ResearchEvent>(),
        Array.Empty<ExplorationEvent>(),
        Array.Empty<CombatEvent>(),
        Array.Empty<ColonizationEvent>());
}
