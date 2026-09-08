using System;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

/// <summary>
/// Externally allocated Combat durability restoration. The caller is authoritative for where
/// these points came from (repair yard, logistics capacity, spare parts, Industry, etc.).
/// </summary>
public sealed record FleetCombatRepairAllocation(
    int FleetId,
    double ShieldPoints,
    double ArmorPoints,
    double HullPoints)
{
    public double TotalRequested => ShieldPoints + ArmorPoints + HullPoints;
}

public sealed record FleetCombatRepairApplicationResult(
    int FleetId,
    bool Accepted,
    string Message,
    double RequestedShields,
    double RequestedArmor,
    double RequestedHull,
    double AppliedShields,
    double AppliedArmor,
    double AppliedHull)
{
    public double TotalRequested => RequestedShields + RequestedArmor + RequestedHull;
    public double TotalApplied => AppliedShields + AppliedArmor + AppliedHull;
    public double UnusedShields => Math.Max(0.0, RequestedShields - AppliedShields);
    public double UnusedArmor => Math.Max(0.0, RequestedArmor - AppliedArmor);
    public double UnusedHull => Math.Max(0.0, RequestedHull - AppliedHull);
    public double TotalUnused => UnusedShields + UnusedArmor + UnusedHull;
}

/// <summary>
/// Applies an explicit externally-budgeted repair allocation to authoritative Combat state.
/// This service never creates repair capacity, never spends Economy/Logistics resources, and
/// never decides whether a repair facility is available. It only clamps supplied restoration
/// to the target vessel's actual physical durability deficits.
/// </summary>
public static class CombatRepairApplicationService
{
    private const double Epsilon = 0.0000001;

    public static FleetCombatRepairApplicationResult Apply(
        GalaxyState galaxy,
        int civilizationId,
        FleetCombatRepairAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(allocation);

        if (!IsValidAmount(allocation.ShieldPoints) ||
            !IsValidAmount(allocation.ArmorPoints) ||
            !IsValidAmount(allocation.HullPoints))
        {
            return Rejected(allocation, "Repair allocation values must be finite and non-negative.");
        }

        if (allocation.TotalRequested <= Epsilon)
            return Rejected(allocation, "Repair allocation must contain a positive durability amount.");

        var fleet = galaxy.Fleets.FirstOrDefault(candidate =>
            candidate.Id == allocation.FleetId &&
            candidate.CivilizationId == civilizationId &&
            candidate.IsActive);
        if (fleet is null)
            return Rejected(allocation, "No active owned fleet with that identity is available for repair.");

        var state = fleet.Combat;
        if (state is null || !CombatProfileRegistry.TryGet(state.ProfileId, out var profile))
            return Rejected(allocation, "The fleet has no valid repairable Combat state.");

        var shields = ClampFinite(state.Shields, profile.MaxShields);
        var armor = ClampFinite(state.Armor, profile.MaxArmor);
        var hull = ClampFinite(state.Hull, profile.MaxHull);

        var shieldDeficit = Math.Max(0.0, profile.MaxShields - shields);
        var armorDeficit = Math.Max(0.0, profile.MaxArmor - armor);
        var hullDeficit = Math.Max(0.0, profile.MaxHull - hull);

        var appliedShields = Math.Min(allocation.ShieldPoints, shieldDeficit);
        var appliedArmor = Math.Min(allocation.ArmorPoints, armorDeficit);
        var appliedHull = Math.Min(allocation.HullPoints, hullDeficit);
        var totalApplied = appliedShields + appliedArmor + appliedHull;
        if (totalApplied <= Epsilon)
            return Rejected(allocation, "The allocation does not match any current repairable durability deficit.");

        state.Shields = shields + appliedShields;
        state.Armor = armor + appliedArmor;
        state.Hull = hull + appliedHull;

        return new FleetCombatRepairApplicationResult(
            fleet.Id,
            true,
            $"Applied {totalApplied:0.###} Combat durability points to {fleet.Name}.",
            allocation.ShieldPoints,
            allocation.ArmorPoints,
            allocation.HullPoints,
            appliedShields,
            appliedArmor,
            appliedHull);
    }

    private static FleetCombatRepairApplicationResult Rejected(
        FleetCombatRepairAllocation allocation,
        string message) =>
        new(
            allocation.FleetId,
            false,
            message,
            allocation.ShieldPoints,
            allocation.ArmorPoints,
            allocation.HullPoints,
            0.0,
            0.0,
            0.0);

    private static bool IsValidAmount(double value) => double.IsFinite(value) && value >= 0.0;

    private static double ClampFinite(double value, double maximum) =>
        Math.Clamp(double.IsFinite(value) ? value : 0.0, 0.0, maximum);
}
