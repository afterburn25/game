using System;
using System.Linq;
using Game.Diagnostics;
using Game.Simulation;
using Game.Simulation.Combat;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Diplomacy;

namespace Game.Presentation;

public partial class Main
{
    private CampaignMassiveCombat? _massiveCombat;
    private readonly MassiveCombatClock _tacticalClock = new();
    private SimulationClock.SpeedLevel _preCombatStrategicSpeed = SimulationClock.SpeedLevel.Normal;
    private bool _tacticalClockOwnsPause;

    public bool UiIsMassiveCombatActive => _galaxy?.ActiveCombatEncounter is { Reconciled: false };
    public int? UiMassiveCombatObserverCivilizationId => UiIsMassiveCombatActive ? _galaxy.PlayerCivilizationId : null;
    public double UiTacticalSpeed => UiIsMassiveCombatActive ? _tacticalClock.SpeedMultiplier : 0;
    public MassiveCombatSnapshot? UiMassiveCombatSnapshot => UiIsMassiveCombatActive
        ? MassiveCombatRuntime().Observe(_galaxy, _galaxy.PlayerCivilizationId, HasCombatScanner(_galaxy.PlayerCivilizationId))
        : null;

    public void UiSetTacticalSpeed(double multiplier, bool announce = true)
    {
        if (!UiIsMassiveCombatActive || !MassiveCombatClock.AllowedSpeeds.Contains(multiplier)) return;
        _tacticalClock.SetSpeed(multiplier);
        if (announce) SetStatus(multiplier == 0 ? "Tactical combat paused." : $"Tactical combat speed set to {multiplier:0.##}×.");
        QueueRedraw();
    }

    public MassiveCombatOrderResult UiIssueMassiveCombatOrder(MassiveCombatOrder order)
    {
        if (!UiIsMassiveCombatActive)
            return new(false, "There is no active tactical encounter.");
        return MassiveCombatRuntime().Engine.IssueOrder(
            _galaxy.ActiveCombatEncounter!.Battle,
            _galaxy.PlayerCivilizationId,
            order);
    }

    private CombatOrderResult BeginMassiveCombat(int fleetId)
    {
        if (UiIsMassiveCombatActive)
        {
            var encounter = _galaxy.ActiveCombatEncounter!;
            var formation = encounter.Battle.Formations.FirstOrDefault(x => x.FleetId == fleetId && x.Active &&
                x.CivilizationId == _galaxy.PlayerCivilizationId);
            if (formation is null) return new(false, "That vessel is not an active formation in this encounter.");
            var contact = UiMassiveCombatSnapshot!.Formations.Where(x => x.CivilizationId != _galaxy.PlayerCivilizationId)
                .OrderBy(x => System.Numerics.Vector2.DistanceSquared(x.Position, formation.Position)).ThenBy(x => x.FormationId).FirstOrDefault();
            if (contact is null) return new(false, "No detected hostile formation is available to engage.");
            var order = MassiveCombatRuntime().Engine.IssueOrder(encounter.Battle, _galaxy.PlayerCivilizationId,
                new(formation.Id, MassiveCombatOrderType.Engage, contact.FormationId));
            return new(order.Accepted, order.Message);
        }
        var result = MassiveCombatRuntime().Begin(_galaxy, _galaxy.PlayerCivilizationId, fleetId, _clock.SimulationDays);
        if (!result.Accepted) return result;
        _preCombatStrategicSpeed = _clock.Speed;
        _clock.SetSpeed(SimulationClock.SpeedLevel.Paused);
        _tacticalClockOwnsPause = true; _tacticalClock.SetSpeed(1);
        PublishPlayerNotification("Combat", result.Message);
        return result;
    }

    protected bool RunMassiveCombatFrame(double delta)
    {
        if (!UiIsMassiveCombatActive) return false;
        if (!_tacticalClockOwnsPause)
        {
            _preCombatStrategicSpeed = _clock.Speed;
            _clock.SetSpeed(SimulationClock.SpeedLevel.Paused);
            _tacticalClockOwnsPause = true;
        }
        var realDelta = Math.Max(0, double.IsFinite(delta) ? delta : 0);
        var accepted = _tacticalClock.AcceptFrame(realDelta);
        var events = accepted > 0
            ? MassiveCombatRuntime().Advance(_galaxy, accepted, HasCombatScanner)
            : MassiveCombatRuntime().Reconcile(_galaxy);
        HandleCombatEvents(events);
        if (_galaxy.ActiveCombatEncounter is { Reconciled: true })
        {
            _clock.SetSpeed(_preCombatStrategicSpeed);
            _tacticalClockOwnsPause = false;
            SetStatus("Tactical encounter complete. Strategic simulation resumed.", 6);
        }
        _statusTimer = Math.Max(0, _statusTimer - realDelta);
        QueueRedraw();
        return true;
    }

    public double? UiObservedCombatPower(int fleetId)
    {
        var fleet = _galaxy.Fleets.FirstOrDefault(x => x.Id == fleetId && x.IsActive);
        if (fleet is null) return null;
        if (fleet.CivilizationId != _galaxy.PlayerCivilizationId && HasCombatScanner(_galaxy.PlayerCivilizationId))
            FleetCombatPower.Observe(_galaxy, _galaxy.PlayerCivilizationId, fleet, _clock.SimulationDays, engaged: false, scanningCapability: true);
        return FleetCombatPower.ObservedPower(_galaxy, _galaxy.PlayerCivilizationId, fleet);
    }

    private bool HasCombatScanner(int civilizationId)
    {
        if (_adaptiveResearch is null || !_adaptiveResearch.Civilizations.TryGetValue(civilizationId, out var research)) return false;
        return research.HasCapability("tech:quantum_sensors") || research.HasCapability("tech:distributed_sensor_network");
    }

    private CampaignMassiveCombat MassiveCombatRuntime() => _massiveCombat ??=
        new CampaignMassiveCombat(_diplomacyRuntime?.HostilityView ?? new DiplomacyCombatHostilityView(_diplomacyState));

    private MassiveCombatOrderResult IssueMassiveFleetOrder(int fleetId, MilitaryOrderType orderType)
    {
        var encounter = _galaxy.ActiveCombatEncounter!;
        var formation = encounter.Battle.Formations.FirstOrDefault(x => x.FleetId == fleetId);
        if (formation is null) return new(false, "That vessel is not part of this tactical encounter.");
        var type = orderType switch
        {
            MilitaryOrderType.Hold => MassiveCombatOrderType.Hold,
            MilitaryOrderType.Defend => MassiveCombatOrderType.Defend,
            MilitaryOrderType.Retreat => MassiveCombatOrderType.Retreat,
            _ => MassiveCombatOrderType.Hold,
        };
        return MassiveCombatRuntime().Engine.IssueOrder(encounter.Battle, _galaxy.PlayerCivilizationId,
            new(formation.Id, type));
    }
}
