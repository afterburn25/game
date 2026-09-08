using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Physical Combat damage that an external maintenance/logistics system may satisfy.
/// Values are Combat durability points, not credits, Industry, spare parts, time, or cargo units.
/// </summary>
public sealed record FleetCombatRepairDemand(
    int FleetId,
    int CivilizationId,
    int? SystemId,
    string ProfileId,
    double MissingShields,
    double MissingArmor,
    double MissingHull,
    double HullIntegrityRatio)
{
    public double TotalMissingDurability => MissingShields + MissingArmor + MissingHull;
    public bool HasStructuralDamage => MissingHull > 0.0;
}

public sealed record CivilizationCombatRepairDemand(
    int CivilizationId,
    IReadOnlyList<FleetCombatRepairDemand> Fleets,
    double TotalMissingShields,
    double TotalMissingArmor,
    double TotalMissingHull)
{
    public int DamagedVesselCount => Fleets.Count;
    public int HullDamagedVesselCount => Fleets.Count(fleet => fleet.HasStructuralDamage);
    public double TotalMissingDurability => TotalMissingShields + TotalMissingArmor + TotalMissingHull;
}

/// <summary>
/// Non-mutating adapter from authoritative Combat damage into repair demand. It reports what
/// is physically damaged; Logistics/Industry/shipyard systems remain authoritative for whether,
/// where, how quickly, and at what resource cost repairs can occur.
/// </summary>
public static class CombatRepairDemandCalculator
{
    private const double Epsilon = 0.0000001;

    public static CivilizationCombatRepairDemand Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        var needs = new List<FleetCombatRepairDemand>();
        foreach (var fleet in galaxy.Fleets
                     .Where(candidate => candidate.IsActive && candidate.CivilizationId == civilizationId)
                     .OrderBy(candidate => candidate.Id))
        {
            var need = BuildFleetDemand(fleet);
            if (need is not null)
                needs.Add(need);
        }

        return new CivilizationCombatRepairDemand(
            civilizationId,
            needs,
            needs.Sum(need => need.MissingShields),
            needs.Sum(need => need.MissingArmor),
            needs.Sum(need => need.MissingHull));
    }

    public static FleetCombatRepairDemand? BuildFleetDemand(FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        if (!fleet.IsActive)
            return null;

        var state = fleet.Combat;
        if (state is null)
            return null; // Missing state is equivalent to the pristine role default.

        if (!CombatProfileRegistry.TryGet(state.ProfileId, out var profile))
            return null; // EnsureState would replace an unknown profile with a pristine role default.

        var shields = Math.Clamp(FiniteOrZero(state.Shields), 0.0, profile.MaxShields);
        var armor = Math.Clamp(FiniteOrZero(state.Armor), 0.0, profile.MaxArmor);
        var hull = Math.Clamp(FiniteOrZero(state.Hull), 0.0, profile.MaxHull);

        var missingShields = Math.Max(0.0, profile.MaxShields - shields);
        var missingArmor = Math.Max(0.0, profile.MaxArmor - armor);
        var missingHull = Math.Max(0.0, profile.MaxHull - hull);
        var totalMissing = missingShields + missingArmor + missingHull;
        if (totalMissing <= Epsilon)
            return null;

        var hullIntegrity = profile.MaxHull <= Epsilon
            ? 1.0
            : Math.Clamp(hull / profile.MaxHull, 0.0, 1.0);

        return new FleetCombatRepairDemand(
            fleet.Id,
            fleet.CivilizationId,
            fleet.CurrentSystemId,
            profile.Id,
            missingShields,
            missingArmor,
            missingHull,
            hullIntegrity);
    }

    private static double FiniteOrZero(double value) => double.IsFinite(value) ? value : 0.0;
}
