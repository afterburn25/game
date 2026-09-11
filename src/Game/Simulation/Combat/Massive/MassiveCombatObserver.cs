using System;
using System.Linq;

namespace Game.Simulation.Combat.Massive;

public static class MassiveCombatObserver
{
    public static MassiveCombatSnapshot BuildSnapshot(MassiveCombatBattleState battle, int observerCivilizationId, IMassiveCombatSensorView sensors)
    {
        ArgumentNullException.ThrowIfNull(battle); ArgumentNullException.ThrowIfNull(sensors);
        var formations = battle.Formations.Where(x => x.Active &&
            (x.CivilizationId == observerCivilizationId || sensors.Confidence(observerCivilizationId, x.Id) > 0)).OrderBy(x => x.Id).Select(formation =>
        {
            var own = formation.CivilizationId == observerCivilizationId;
            var confidence = own ? 1f : Math.Clamp(sensors.Confidence(observerCivilizationId, formation.Id), 0, 1);
            var exact = own || confidence >= .999f;
            var count = formation.ActiveShipCount;
            var uncertainty = exact ? 0 : Math.Max(1, (int)MathF.Ceiling(count * (1f - confidence) * .45f));
            float? strengthLow = null, strengthHigh = null;
            if (own || sensors.CanEstimateCombatPower(observerCivilizationId, formation.Id))
            {
                var strength = MassiveCombatPowerCalculator.FormationPower(formation);
                var spread = exact ? 0 : strength * (1f - confidence) * .5f;
                strengthLow = Math.Max(0, strength - spread); strengthHigh = strength + spread;
            }
            var mayIdentify = own || sensors.IdentifiesImportantVessels(observerCivilizationId, formation.Id);
            var mayEstimatePower = own || sensors.CanEstimateCombatPower(observerCivilizationId, formation.Id);
            var important = mayIdentify
                ? formation.ImportantVessels.Where(x => !x.Destroyed && !x.Escaped).OrderBy(x => x.Id)
                    .Select(x => new MassiveObservedVessel(x.Id, x.Name, x.DesignId,
                        mayEstimatePower ? MassiveCombatPowerCalculator.ImportantVesselPower(formation.Loadout, x) : null,
                        x.IsFlagship, x.IsCarrier, x.IsInterdictor, x.HullFraction < .3f)).ToArray()
                : Array.Empty<MassiveObservedVessel>();
            return new MassiveObservedFormation(formation.Id, formation.CivilizationId,
                own || confidence >= .65f ? formation.Name : "Unidentified formation", formation.Position, formation.Velocity,
                formation.Shape, Math.Max(0, count - uncertainty), count + uncertainty, strengthLow, strengthHigh,
                exact, mayIdentify && formation.Loadout.Modules.Any(x => x.Kind == MassiveModuleKind.WarpInterdictor && x.Enabled && x.Condition > .05f),
                own && formation.WarpBlocked, own ? formation.WarpSpoolProgress : 0,
                mayEstimatePower ? MassiveCombatPowerCalculator.EffectivePerShipPower(formation) : null, important);
        }).ToArray();
        var visibleEvents = battle.Events
            .Where(x => x.ActorCivilizationId == observerCivilizationId || x.TargetCivilizationId == observerCivilizationId ||
                sensors.Confidence(observerCivilizationId, x.ActorFormationId) >= .65f)
            .TakeLast(128)
            .Select(x => ObserveEvent(x, observerCivilizationId, sensors))
            .ToArray();
        return new(battle.BattleId, battle.Tick, battle.SimulatedSeconds,
            battle.Formations.Where(x => x.CivilizationId == observerCivilizationId).Sum(x => x.ActiveShipCount), formations, visibleEvents);
    }

    private static MassiveObservedCombatEvent ObserveEvent(MassiveCombatEvent value, int observerCivilizationId, IMassiveCombatSensorView sensors)
    {
        var actorKnown = value.ActorCivilizationId == observerCivilizationId || sensors.Confidence(observerCivilizationId, value.ActorFormationId) >= .65f;
        var targetKnown = value.TargetFormationId is null || value.TargetCivilizationId == observerCivilizationId ||
            sensors.Confidence(observerCivilizationId, value.TargetFormationId.Value) >= .65f;
        if (actorKnown && targetKnown)
            return new(value.Sequence, value.Tick, value.Type, value.ActorCivilizationId, value.ActorFormationId,
                value.TargetCivilizationId, value.TargetFormationId, value.Magnitude, value.Position, value.Message, true);

        // A hit on an owned unit is observable, but it cannot reveal an unscanned attacker's
        // identity, location, weapon volume, or formation name through an event side channel.
        return new(value.Sequence, value.Tick, value.Type, null, null,
            value.TargetCivilizationId == observerCivilizationId ? value.TargetCivilizationId : null,
            value.TargetCivilizationId == observerCivilizationId ? value.TargetFormationId : null,
            null, null, "An unidentified hostile action affected a friendly formation.", false);
    }
}
