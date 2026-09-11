using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

public sealed record FleetPowerObservation(int ObserverId, int FleetId, double Power, double ObservedDay, string Evidence);

/// <summary>One display rating, calculated from real installed equipment and present damage; not an auto-resolve formula.</summary>
public static class FleetCombatPower
{
    public const int MaximumObservations = 4096;
    public const int MaximumObservationsPerObserver = 2048;
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
        => ObserveMany(galaxy, observerId, [target], day, engaged, scanningCapability);

    public static void ObserveMany(GalaxyState galaxy, int observerId, IEnumerable<FleetState> targets, double day,
        bool engaged, bool scanningCapability)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(targets);
        if (observerId < 0 || !double.IsFinite(day) || day < 0)
            throw new ArgumentOutOfRangeException(nameof(day), "Combat intelligence requires valid campaign identities and time.");
        if (!engaged && !scanningCapability) return;
        var fleetMap = galaxy.Fleets.ToDictionary(x => x.Id);
        var observed = targets.OrderBy(x => x.Id).Take(MaximumObservationsPerObserver).ToArray();
        if (observed.Any(target => !fleetMap.TryGetValue(target.Id, out var member) || !ReferenceEquals(member, target)))
            throw new ArgumentOutOfRangeException(nameof(targets), "Combat intelligence target is not a campaign fleet.");
        var observedIds = observed.Select(x => x.Id).ToHashSet();
        galaxy.CombatIntelligence.RemoveAll(x => x.ObserverId == observerId && observedIds.Contains(x.FleetId));
        galaxy.CombatIntelligence.AddRange(observed.Select(target => new FleetPowerObservation(observerId, target.Id,
            OwnPower(target), day, engaged ? "Engagement" : "Combat scanner")));
        var observerOverflow = galaxy.CombatIntelligence.Count(x => x.ObserverId == observerId) - MaximumObservationsPerObserver;
        if (observerOverflow > 0)
        {
            var evict = galaxy.CombatIntelligence.Where(x => x.ObserverId == observerId)
                .OrderBy(x => x.ObservedDay).ThenBy(x => x.FleetId).Take(observerOverflow).ToHashSet();
            galaxy.CombatIntelligence.RemoveAll(evict.Contains);
        }
        // Retain bounded intelligence, not every historic reading of every vessel.
        if (galaxy.CombatIntelligence.Count > MaximumObservations)
            galaxy.CombatIntelligence.RemoveRange(0, galaxy.CombatIntelligence.Count - MaximumObservations);
    }
}
