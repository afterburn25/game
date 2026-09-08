using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Authoritative physical military-presence summary for subsystem integration.
/// This is not a fair-information/player-intelligence view: presentation code must not
/// use it to reveal exact hidden foreign force strength.
/// </summary>
public sealed record SystemMilitaryPresenceSummary(
    int CivilizationId,
    int SystemId,
    SystemMilitaryPosture Posture,
    int OwnArmedVessels,
    int HostileArmedVessels,
    int NonHostileForeignArmedVessels,
    int HostileCivilizations,
    double OwnCurrentStrength,
    double HostileCurrentStrength)
{
    public bool HasHostileInterdiction => HostileArmedVessels > 0;
    public bool IsContested => OwnArmedVessels > 0 && HostileArmedVessels > 0;
}

public enum SystemMilitaryPosture
{
    Clear,
    Secured,
    Interdicted,
    Contested,
}

/// <summary>
/// Reconstructible, non-persisted calculation of combat-effective armed presence in one system.
/// Retreating/disengaged/destroyed vessels do not exert military control. Political hostility
/// is consulted once per represented foreign civilization rather than once per vessel.
/// </summary>
public static class CombatSystemPresenceCalculator
{
    private const double Epsilon = 0.0000001;

    public static SystemMilitaryPresenceSummary Assess(
        GalaxyState galaxy,
        ICombatHostilityView hostilityView,
        int civilizationId,
        int systemId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(hostilityView);

        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");
        if (!galaxy.Systems.Any(system => system.Id == systemId))
            throw new InvalidOperationException($"Unknown system {systemId}.");

        var groups = galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CurrentSystemId == systemId)
            .Select(TryBuildPresence)
            .Where(presence => presence is not null)
            .Select(presence => presence!)
            .GroupBy(presence => presence.CivilizationId)
            .OrderBy(group => group.Key)
            .ToArray();

        var ownCount = 0;
        var hostileCount = 0;
        var nonHostileForeignCount = 0;
        var hostileCivilizations = 0;
        var ownStrength = 0.0;
        var hostileStrength = 0.0;

        foreach (var group in groups)
        {
            var count = group.Count();
            var strength = group.Sum(presence => presence.CurrentStrength);

            if (group.Key == civilizationId)
            {
                ownCount += count;
                ownStrength += strength;
                continue;
            }

            // Interdiction against the assessed civilization exists only when the foreign
            // civilization is currently politically permitted to initiate hostile Combat.
            if (hostilityView.AreHostile(group.Key, civilizationId))
            {
                hostileCivilizations++;
                hostileCount += count;
                hostileStrength += strength;
            }
            else
            {
                nonHostileForeignCount += count;
            }
        }

        var posture = (ownCount > 0, hostileCount > 0) switch
        {
            (false, false) => SystemMilitaryPosture.Clear,
            (true, false) => SystemMilitaryPosture.Secured,
            (false, true) => SystemMilitaryPosture.Interdicted,
            _ => SystemMilitaryPosture.Contested,
        };

        return new SystemMilitaryPresenceSummary(
            civilizationId,
            systemId,
            posture,
            ownCount,
            hostileCount,
            nonHostileForeignCount,
            hostileCivilizations,
            ownStrength,
            hostileStrength);
    }

    private static VesselPresence? TryBuildPresence(FleetState fleet)
    {
        var source = fleet.Combat;
        CombatProfileDefinition profile;
        if (source is not null && CombatProfileRegistry.TryGet(source.ProfileId, out var resolvedProfile))
            profile = resolvedProfile;
        else
            profile = CombatProfileRegistry.Get(CombatProfileRegistry.DefaultProfileId(fleet.Role));

        if (!profile.HasWeapon)
            return null;

        var hull = source is null ? profile.MaxHull : Math.Clamp(source.Hull, 0.0, profile.MaxHull);
        if (hull <= Epsilon)
            return null;

        var order = source is null || !Enum.IsDefined(source.Order)
            ? MilitaryOrderType.Hold
            : source.Order;
        if (order == MilitaryOrderType.Retreat)
            return null;

        if (source is { IsDisengaged: true } && source.DisengagedSystemId == fleet.CurrentSystemId)
            return null;

        var shields = source is null ? profile.MaxShields : Math.Clamp(source.Shields, 0.0, profile.MaxShields);
        var armor = source is null ? profile.MaxArmor : Math.Clamp(source.Armor, 0.0, profile.MaxArmor);
        var currentDurability = shields + armor + hull;
        var hullReadiness = profile.MaxHull <= Epsilon ? 0.0 : Math.Clamp(hull / profile.MaxHull, 0.0, 1.0);
        var currentStrength = currentDurability + profile.SustainedDamagePerDay * 3.0 * hullReadiness;

        return new VesselPresence(fleet.CivilizationId, currentStrength);
    }

    private sealed record VesselPresence(int CivilizationId, double CurrentStrength);
}
