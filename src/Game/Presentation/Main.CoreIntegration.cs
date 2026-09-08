using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Diagnostics;
using Game.Simulation;
using Game.Simulation.AI;
using Game.Simulation.Combat;
using Game.Simulation.Diplomacy;
using Game.Simulation.Time;

namespace Game.Presentation;

/// <summary>
/// Core-integration bridge for the existing Main presentation class. Authoritative state
/// mutation is delegated to GalaxySimulationStepCoordinator; this partial handles only the
/// Godot-facing clock, campaign-level Diplomacy bridges, event presentation, diagnostics and redraw work.
/// </summary>
public partial class Main
{
    private const double DiplomacyTicksPerSimulationDay = 1000.0;

    private DiplomacyState _diplomacyState = new();
    private GalaxySimulationStepCoordinator _coreSimulation = new();

    private void RebuildIntegratedCoreSimulation()
    {
        var combat = new CombatSimulation(new DiplomacyCombatHostilityView(_diplomacyState));
        var strategicAi = new CivilizationStrategicRuntimeCoordinator(
            knowledgeProvider: new DiplomacyStrategicKnowledgeProvider(_diplomacyState));
        _coreSimulation = new GalaxySimulationStepCoordinator(combat: combat, strategicAi: strategicAi);
    }

    protected void RunIntegratedSimulationFrame(double delta)
    {
        if (_galaxy is null)
            return;

        var simulationDays = _clock.Advance(delta);
        var step = _coreSimulation.Advance(_galaxy, simulationDays);
        ApplyIntegratedDiplomacyEvents(step);

        HandleConstructionEvents(step.ConstructionEvents);
        HandleShipbuildingEvents(step.ShipbuildingEvents);
        HandleResearchEvents(step.ResearchEvents);
        HandleExplorationEvents(step.ExplorationEvents);
        HandleCombatEvents(step.CombatEvents);
        HandleColonizationEvents(step.ColonizationEvents);

        _performanceLogTimer += delta;
        _statusTimer = Math.Max(0.0, _statusTimer - delta);

        if (_performanceLogTimer >= 5.0)
        {
            _performanceLogTimer = 0.0;
            var playerAllocation = step.IndustryAllocations.FirstOrDefault(allocation => allocation.CivilizationId == _galaxy.PlayerCivilizationId);
            var industryAllocation = playerAllocation is null
                ? "industryAllocation=idle"
                : $"industryAllocation=construction:{playerAllocation.ConstructionAllocated:0.0},shipbuilding:{playerAllocation.ShipbuildingAllocated:0.0}";

            SupportLogger.Log(
                "performance",
                $"date={CampaignCalendar.FormatDate(_clock.SimulationDays)} fps={Engine.GetFramesPerSecond()} requested={_clock.RequestedMultiplier:0.00}x effective={_clock.EffectiveMultiplier:0.00}x backlogDays={_clock.BacklogDays:0.000} managedMemory={GC.GetTotalMemory(false)} fleets={_galaxy.Fleets.Count(f => f.IsActive)} colonies={_galaxy.Colonies.Count} industry={PlayerEconomy.Industry:0.0} science={PlayerEconomy.Science:0.0} {industryAllocation}");
        }

        QueueRedraw();
    }

    protected void RefreshIntegratedShipbuildingPresentation()
    {
        if (_galaxy is null)
            return;

        EnsureShipbuildingHud();
        UpdateShipbuildingHud();
        UpdateScienceFleetMarkers();
    }

    private void ApplyIntegratedDiplomacyEvents(SimulationStepResult step)
    {
        if (step.ExplorationEvents.Count == 0 && step.CombatEvents.Count == 0)
            return;

        var tick = ToDiplomacyTick(_clock.SimulationDays);
        if (step.ExplorationEvents.Count > 0)
        {
            var diplomacy = new DiplomacySimulation(_diplomacyState);
            new ExplorationDiplomacyBridge(diplomacy).Process(step.ExplorationEvents, tick);
        }

        if (step.CombatEvents.Count > 0)
            new CombatDiplomacyBridge(_diplomacyState).Process(step.CombatEvents, tick);
    }

    private static long ToDiplomacyTick(double simulationDays)
    {
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays));

        var scaled = Math.Floor(simulationDays * DiplomacyTicksPerSimulationDay);
        return scaled >= long.MaxValue ? long.MaxValue : (long)scaled;
    }

    private void HandleCombatEvents(IReadOnlyList<CombatEvent> events)
    {
        var playerId = _galaxy.PlayerCivilizationId;
        foreach (var combatEvent in events)
        {
            // Presentation deliberately receives only player-involved combat for now.
            // Hidden third-party battles must not leak through status text or diagnostics.
            var playerInvolved = combatEvent.ActorCivilizationId == playerId ||
                                 combatEvent.TargetCivilizationId == playerId;
            if (!playerInvolved)
                continue;

            SupportLogger.Log(
                "combat",
                $"type={combatEvent.Type} system={combatEvent.SystemId?.ToString() ?? "none"} actorCiv={combatEvent.ActorCivilizationId} actorFleet={combatEvent.ActorFleetId} targetCiv={combatEvent.TargetCivilizationId?.ToString() ?? "none"} targetFleet={combatEvent.TargetFleetId?.ToString() ?? "none"} message={combatEvent.Message}");

            if (combatEvent.Type is CombatEventType.EngagementStarted or
                CombatEventType.FleetRetreatInitiated or
                CombatEventType.FleetEscaped or
                CombatEventType.FleetDestroyed or
                CombatEventType.EngagementEnded)
            {
                SetStatus(combatEvent.Message, 6.0);
            }
        }
    }
}
