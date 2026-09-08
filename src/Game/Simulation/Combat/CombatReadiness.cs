using System;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Exact own-force Combat readiness. This is suitable for the owning civilization's UI/AI;
/// it must not be reused as an exact foreign-intelligence view.
/// </summary>
public sealed record CombatReadinessSummary(
    int CivilizationId,
    int ActiveVessels,
    int ActiveArmedVessels,
    int CombatEffectiveArmedVessels,
    int DamagedVessels,
    int HullDamagedVessels,
    int RetreatingVessels,
    int DisengagedVessels,
    double CurrentDurability,
    double MaximumDurability,
    double CurrentArmedStrength,
    double MaximumArmedStrength,
    double CombatEffectiveArmedStrength,
    double TotalRepairDeficit)
{
    public double DurabilityRatio => MaximumDurability <= 0.0
        ? 1.0
        : Math.Clamp(CurrentDurability / MaximumDurability, 0.0, 1.0);

    public double ArmedStrengthRatio => MaximumArmedStrength <= 0.0
        ? 1.0
        : Math.Clamp(CurrentArmedStrength / MaximumArmedStrength, 0.0, 1.0);
}

/// <summary>
/// Assembly-local non-mutating evaluation of one physical vessel. Combat read models share this
/// so readiness, system control and future operational views do not drift into different strength
/// or retreat/disengagement semantics.
/// </summary>
internal sealed record FleetCombatReadinessSnapshot(
    double CurrentDurability,
    double MaximumDurability,
    double RepairDeficit,
    double MissingHull,
    double CurrentStrength,
    double MaximumStrength,
    bool IsArmed,
    bool IsCombatEffective,
    bool IsRetreating,
    bool IsDisengaged);

/// <summary>
/// Non-mutating exact-own-state aggregation. Missing or unknown Combat state is evaluated as the
/// pristine role default, matching the effective fallback semantics of CombatProfileRegistry
/// without rewriting legacy/invalid source state during a read.
/// </summary>
public static class CombatReadinessCalculator
{
    private const double Epsilon = 0.0000001;

    public static CombatReadinessSummary Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        var activeVessels = 0;
        var armedVessels = 0;
        var effectiveArmedVessels = 0;
        var damagedVessels = 0;
        var hullDamagedVessels = 0;
        var retreatingVessels = 0;
        var disengagedVessels = 0;
        var currentDurability = 0.0;
        var maximumDurability = 0.0;
        var currentArmedStrength = 0.0;
        var maximumArmedStrength = 0.0;
        var effectiveArmedStrength = 0.0;
        var repairDeficit = 0.0;

        foreach (var fleet in galaxy.Fleets
                     .Where(candidate => candidate.IsActive && candidate.CivilizationId == civilizationId)
                     .OrderBy(candidate => candidate.Id))
        {
            activeVessels++;
            var snapshot = ReadFleet(fleet);

            currentDurability += snapshot.CurrentDurability;
            maximumDurability += snapshot.MaximumDurability;
            repairDeficit += snapshot.RepairDeficit;

            if (snapshot.RepairDeficit > Epsilon)
                damagedVessels++;
            if (snapshot.MissingHull > Epsilon)
                hullDamagedVessels++;
            if (snapshot.IsRetreating)
                retreatingVessels++;
            if (snapshot.IsDisengaged)
                disengagedVessels++;

            if (!snapshot.IsArmed)
                continue;

            armedVessels++;
            currentArmedStrength += snapshot.CurrentStrength;
            maximumArmedStrength += snapshot.MaximumStrength;

            if (!snapshot.IsCombatEffective)
                continue;

            effectiveArmedVessels++;
            effectiveArmedStrength += snapshot.CurrentStrength;
        }

        return new CombatReadinessSummary(
            civilizationId,
            activeVessels,
            armedVessels,
            effectiveArmedVessels,
            damagedVessels,
            hullDamagedVessels,
            retreatingVessels,
            disengagedVessels,
            currentDurability,
            maximumDurability,
            currentArmedStrength,
            maximumArmedStrength,
            effectiveArmedStrength,
            repairDeficit);
    }

    internal static FleetCombatReadinessSnapshot ReadFleet(FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);

        var state = fleet.Combat;
        CombatProfileDefinition profile;
        var usesPersistedState = false;

        if (state is not null && CombatProfileRegistry.TryGet(state.ProfileId, out var persistedProfile))
        {
            profile = persistedProfile;
            usesPersistedState = true;
        }
        else
        {
            profile = CombatProfileRegistry.Get(CombatProfileRegistry.DefaultProfileId(fleet.Role));
        }

        var shields = usesPersistedState
            ? ClampFinite(state!.Shields, profile.MaxShields)
            : profile.MaxShields;
        var armor = usesPersistedState
            ? ClampFinite(state!.Armor, profile.MaxArmor)
            : profile.MaxArmor;
        var hull = usesPersistedState
            ? ClampFinite(state!.Hull, profile.MaxHull)
            : profile.MaxHull;

        var currentDurability = shields + armor + hull;
        var maximumDurability = profile.MaxShields + profile.MaxArmor + profile.MaxHull;
        var missingHull = Math.Max(0.0, profile.MaxHull - hull);
        var repairDeficit = Math.Max(0.0, maximumDurability - currentDurability);

        var order = usesPersistedState && Enum.IsDefined(state!.Order)
            ? state.Order
            : MilitaryOrderType.Hold;
        var isRetreating = order == MilitaryOrderType.Retreat;
        var isDisengaged = usesPersistedState &&
                           state!.IsDisengaged &&
                           state.DisengagedSystemId == fleet.CurrentSystemId;

        var hullReadiness = profile.MaxHull <= Epsilon
            ? 0.0
            : Math.Clamp(hull / profile.MaxHull, 0.0, 1.0);
        var maximumOffense = profile.SustainedDamagePerDay * 3.0;
        var currentStrength = currentDurability + maximumOffense * hullReadiness;
        var maximumStrength = maximumDurability + maximumOffense;
        var isArmed = profile.HasWeapon;
        var isCombatEffective = isArmed && hull > Epsilon && !isRetreating && !isDisengaged;

        return new FleetCombatReadinessSnapshot(
            currentDurability,
            maximumDurability,
            repairDeficit,
            missingHull,
            currentStrength,
            maximumStrength,
            isArmed,
            isCombatEffective,
            isRetreating,
            isDisengaged);
    }

    private static double ClampFinite(double value, double maximum) =>
        Math.Clamp(double.IsFinite(value) ? value : 0.0, 0.0, maximum);
}
