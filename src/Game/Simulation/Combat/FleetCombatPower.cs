using System;
using System.Linq;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

public sealed record FleetPowerObservation(int ObserverId, int FleetId, double Power, double ObservedDay, string Evidence);

/// <summary>One display rating, calculated from real installed equipment and present damage; not an auto-resolve formula.</summary>
public static class FleetCombatPower
{
    public static double OwnPower(FleetState fleet)
    {
        if (!fleet.IsActive) return 0;
        var profile = CombatProfileRegistry.TryGet(fleet.Combat?.ProfileId ?? string.Empty, out var found)
            ? found : CombatProfileRegistry.Get(CombatProfileRegistry.DefaultProfileId(fleet.Role));
        var loadout = fleet.TacticalLoadout ?? MassiveCombatLoadouts.FromLegacy(profile);
        var maximum = profile.MaxShields + profile.MaxArmor + profile.MaxHull;
        var present = fleet.Combat is { } state ? state.Shields + state.Armor + state.Hull : maximum;
        return MassiveCombatPowerCalculator.PerShipPower(loadout) * Math.Clamp(present / Math.Max(1, maximum), 0, 1);
    }

    public static double? ObservedPower(GalaxyState galaxy, int observerId, FleetState target)
    {
        if (target.CivilizationId == observerId) return OwnPower(target);
        // An observation is a dated reading, never permission to fetch live hidden enemy state.
        return galaxy.CombatIntelligence.LastOrDefault(x => x.ObserverId == observerId && x.FleetId == target.Id)?.Power;
    }

    public static void Observe(GalaxyState galaxy, int observerId, FleetState target, double day, bool engaged, bool scanningCapability)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(target);
        if (observerId < 0 || !double.IsFinite(day) || day < 0 || !galaxy.Fleets.Any(x => x.Id == target.Id))
            throw new ArgumentOutOfRangeException(nameof(day), "Combat intelligence requires valid campaign identities and time.");
        if (!engaged && !scanningCapability) return;
        galaxy.CombatIntelligence.RemoveAll(x => x.ObserverId == observerId && x.FleetId == target.Id);
        galaxy.CombatIntelligence.Add(new(observerId, target.Id, OwnPower(target), day, engaged ? "Engagement" : "Combat scanner"));
        // Retain bounded intelligence, not every historic reading of every vessel.
        if (galaxy.CombatIntelligence.Count > 4096) galaxy.CombatIntelligence.RemoveRange(0, galaxy.CombatIntelligence.Count - 4096);
    }
}
