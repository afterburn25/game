using System;
using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

public static class CombatProfileIds
{
    public const string CivilianLight = "civilian_light_v1";
    public const string CivilianScience = "civilian_science_v1";
    public const string CivilianHeavy = "civilian_heavy_v1";
    public const string PatrolCorvetteMk1 = "patrol_corvette_mk1";
}

public sealed record CombatProfileDefinition(
    string Id,
    double MaxShields,
    double MaxArmor,
    double MaxHull,
    double WeaponDamage,
    double WeaponIntervalDays,
    double RetreatDelayDays)
{
    public bool HasWeapon => WeaponDamage > 0.0 && WeaponIntervalDays > 0.0;
    public double SustainedDamagePerDay => HasWeapon ? WeaponDamage / WeaponIntervalDays : 0.0;
}

/// <summary>
/// Stable data identifiers keep combat logic independent of display names such as
/// "laser" or "missile" while leaving room for later module/weapon definitions.
/// </summary>
public static class CombatProfileRegistry
{
    private static readonly IReadOnlyDictionary<string, CombatProfileDefinition> Profiles =
        new Dictionary<string, CombatProfileDefinition>(StringComparer.Ordinal)
        {
            [CombatProfileIds.CivilianLight] = new(
                CombatProfileIds.CivilianLight,
                MaxShields: 0.0,
                MaxArmor: 10.0,
                MaxHull: 45.0,
                WeaponDamage: 0.0,
                WeaponIntervalDays: 1.0,
                RetreatDelayDays: 0.75),
            [CombatProfileIds.CivilianScience] = new(
                CombatProfileIds.CivilianScience,
                MaxShields: 0.0,
                MaxArmor: 14.0,
                MaxHull: 60.0,
                WeaponDamage: 0.0,
                WeaponIntervalDays: 1.0,
                RetreatDelayDays: 1.0),
            [CombatProfileIds.CivilianHeavy] = new(
                CombatProfileIds.CivilianHeavy,
                MaxShields: 0.0,
                MaxArmor: 24.0,
                MaxHull: 105.0,
                WeaponDamage: 0.0,
                WeaponIntervalDays: 1.0,
                RetreatDelayDays: 1.5),
            [CombatProfileIds.PatrolCorvetteMk1] = new(
                CombatProfileIds.PatrolCorvetteMk1,
                MaxShields: 35.0,
                MaxArmor: 45.0,
                MaxHull: 95.0,
                WeaponDamage: 28.0,
                WeaponIntervalDays: 0.75,
                RetreatDelayDays: 1.5),
        };

    public static CombatProfileDefinition Get(string profileId) => Profiles[profileId];

    public static bool TryGet(string profileId, out CombatProfileDefinition profile) =>
        Profiles.TryGetValue(profileId, out profile!);

    public static string DefaultProfileId(FleetRole role) => role switch
    {
        FleetRole.Military => CombatProfileIds.PatrolCorvetteMk1,
        FleetRole.Science => CombatProfileIds.CivilianScience,
        FleetRole.Colony => CombatProfileIds.CivilianHeavy,
        _ => CombatProfileIds.CivilianLight,
    };

    public static FleetCombatState CreateInitialState(string? profileId, FleetRole role)
    {
        var resolvedId = !string.IsNullOrWhiteSpace(profileId) && Profiles.ContainsKey(profileId)
            ? profileId
            : DefaultProfileId(role);
        var profile = Profiles[resolvedId];
        return new FleetCombatState
        {
            ProfileId = profile.Id,
            Shields = profile.MaxShields,
            Armor = profile.MaxArmor,
            Hull = profile.MaxHull,
        };
    }

    public static FleetCombatState EnsureState(FleetState fleet)
    {
        if (fleet.Combat is null)
        {
            fleet.Combat = CreateInitialState(null, fleet.Role);
            return fleet.Combat;
        }

        var state = fleet.Combat;
        if (!TryGet(state.ProfileId, out var profile))
        {
            state = CreateInitialState(null, fleet.Role);
            fleet.Combat = state;
            return state;
        }

        state.Shields = Math.Clamp(state.Shields, 0.0, profile.MaxShields);
        state.Armor = Math.Clamp(state.Armor, 0.0, profile.MaxArmor);
        state.Hull = Math.Clamp(state.Hull, 0.0, profile.MaxHull);
        state.WeaponCooldownRemainingDays = Math.Max(0.0, state.WeaponCooldownRemainingDays);
        state.RetreatProgressDays = Math.Max(0.0, state.RetreatProgressDays);
        if (!Enum.IsDefined(state.Order))
            state.Order = MilitaryOrderType.Hold;

        if (state.IsDisengaged && state.DisengagedSystemId != fleet.CurrentSystemId)
        {
            state.IsDisengaged = false;
            state.DisengagedSystemId = null;
        }

        return state;
    }
}
