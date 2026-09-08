using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class CombatRepairApplicationValidation
{
    public static void ValidateExternallyBudgetedCombatRepairApplication()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x4150_504C_5952_5052L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var civilization = galaxy.Civilizations[0];
        var foreign = galaxy.Civilizations[1];
        var system = galaxy.Systems[0];

        var patrol = CreateFleet(
            8300,
            civilization.Id,
            "Repair Application Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        patrol.Combat!.Shields = 5.0;
        patrol.Combat.Armor = 20.0;
        patrol.Combat.Hull = 40.0;
        galaxy.Fleets.Add(patrol);

        var pristineLegacy = CreateFleet(
            8301,
            civilization.Id,
            "Repair Application Legacy",
            FleetRole.Military,
            system.Id,
            system.Position,
            combat: null);
        galaxy.Fleets.Add(pristineLegacy);

        var invalidProfile = CreateFleet(
            8302,
            civilization.Id,
            "Repair Application Invalid",
            FleetRole.Military,
            system.Id,
            system.Position,
            new FleetCombatState { ProfileId = "invalid-repair-profile", Hull = 1.0 });
        galaxy.Fleets.Add(invalidProfile);

        var destroyed = CreateFleet(
            8303,
            civilization.Id,
            "Repair Application Destroyed",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        destroyed.IsActive = false;
        destroyed.Combat!.Hull = 0.0;
        galaxy.Fleets.Add(destroyed);

        var foreignPatrol = CreateFleet(
            8400,
            foreign.Id,
            "Repair Application Foreign",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        foreignPatrol.Combat!.Hull = 30.0;
        galaxy.Fleets.Add(foreignPatrol);

        var first = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, ShieldPoints: 100.0, ArmorPoints: 10.0, HullPoints: 30.0));

        Require(first.Accepted, $"valid external repair allocation was rejected: {first.Message}");
        RequireNear(first.AppliedShields, 30.0, "shield repair was not clamped to the real deficit");
        RequireNear(first.AppliedArmor, 10.0, "armor repair application changed");
        RequireNear(first.AppliedHull, 30.0, "hull repair application changed");
        RequireNear(first.TotalApplied, 70.0, "total applied repair changed");
        RequireNear(first.UnusedShields, 70.0, "unused over-allocated shield capacity was not returned");
        RequireNear(first.UnusedArmor, 0.0, "armor allocation unexpectedly went unused");
        RequireNear(first.UnusedHull, 0.0, "hull allocation unexpectedly went unused");
        RequireNear(first.TotalUnused, 70.0, "total unused repair allocation changed");
        RequireNear(patrol.Combat!.Shields, 35.0, "shield repair exceeded or missed profile maximum");
        RequireNear(patrol.Combat.Armor, 30.0, "armor repair did not mutate authoritative Combat state");
        RequireNear(patrol.Combat.Hull, 70.0, "hull repair did not mutate authoritative Combat state");

        var remaining = CombatRepairDemandCalculator.BuildFleetDemand(patrol)
            ?? throw new InvalidOperationException("partially repaired patrol unexpectedly had no remaining repair demand");
        RequireNear(remaining.MissingShields, 0.0, "remaining repair demand retained fully repaired shields");
        RequireNear(remaining.MissingArmor, 15.0, "remaining armor deficit changed after partial repair");
        RequireNear(remaining.MissingHull, 25.0, "remaining hull deficit changed after partial repair");
        RequireNear(remaining.TotalMissingDurability, 40.0, "remaining total repair deficit changed");

        var second = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, ShieldPoints: 0.0, ArmorPoints: 50.0, HullPoints: 50.0));

        Require(second.Accepted, "second valid repair allocation was rejected");
        RequireNear(second.AppliedArmor, 15.0, "second armor repair did not clamp to remaining deficit");
        RequireNear(second.AppliedHull, 25.0, "second hull repair did not clamp to remaining deficit");
        RequireNear(second.UnusedArmor, 35.0, "second armor over-allocation was not exposed as unused");
        RequireNear(second.UnusedHull, 25.0, "second hull over-allocation was not exposed as unused");
        Require(CombatRepairDemandCalculator.BuildFleetDemand(patrol) is null,
            "fully repaired patrol still produced repair demand");

        var fullBefore = (patrol.Combat.Shields, patrol.Combat.Armor, patrol.Combat.Hull);
        var noDeficit = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, 1.0, 0.0, 0.0));
        Require(!noDeficit.Accepted && noDeficit.TotalApplied == 0.0,
            "repair with no matching deficit was treated as consumed repair capacity");
        Require(fullBefore == (patrol.Combat.Shields, patrol.Combat.Armor, patrol.Combat.Hull),
            "rejected no-deficit repair mutated a fully repaired vessel");

        var negative = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, -1.0, 0.0, 0.0));
        Require(!negative.Accepted && negative.TotalApplied == 0.0,
            "negative repair allocation was accepted");

        var nonFinite = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, double.NaN, 0.0, 0.0));
        Require(!nonFinite.Accepted && nonFinite.TotalApplied == 0.0,
            "non-finite repair allocation was accepted");

        var zero = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(patrol.Id, 0.0, 0.0, 0.0));
        Require(!zero.Accepted, "zero repair allocation was accepted");

        var foreignBefore = foreignPatrol.Combat!.Hull;
        var foreignResult = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(foreignPatrol.Id, 0.0, 0.0, 20.0));
        Require(!foreignResult.Accepted && foreignPatrol.Combat.Hull == foreignBefore,
            "repair application crossed civilization ownership");

        var destroyedResult = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(destroyed.Id, 0.0, 0.0, 20.0));
        Require(!destroyedResult.Accepted && destroyed.Combat!.Hull == 0.0,
            "repair application revived an inactive destroyed vessel");

        var legacyResult = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(pristineLegacy.Id, 10.0, 10.0, 10.0));
        Require(!legacyResult.Accepted && pristineLegacy.Combat is null,
            "repair application invented Combat state for a pristine legacy vessel");

        var invalidBefore = invalidProfile.Combat!.ProfileId;
        var invalidResult = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(invalidProfile.Id, 10.0, 10.0, 10.0));
        Require(!invalidResult.Accepted && invalidProfile.Combat.ProfileId == invalidBefore,
            "repair application rewrote an unknown Combat profile");

        var unknownResult = CombatRepairApplicationService.Apply(
            galaxy,
            civilization.Id,
            new FleetCombatRepairAllocation(int.MaxValue, 1.0, 1.0, 1.0));
        Require(!unknownResult.Accepted, "unknown fleet repair allocation was accepted");
    }

    private static FleetState CreateFleet(
        int id,
        int civilizationId,
        string name,
        FleetRole role,
        int systemId,
        Vector2 position,
        FleetCombatState? combat) => new()
    {
        Id = id,
        CivilizationId = civilizationId,
        Name = name,
        Role = role,
        Position = position,
        CurrentSystemId = systemId,
        StrategicSpeed = role == FleetRole.Military ? 21.0 : 18.0,
        SensorRange = role == FleetRole.Military ? 125.0f : 80.0f,
        IsActive = true,
        Combat = combat,
    };

    private static void RequireNear(double actual, double expected, string message, double tolerance = 0.000001)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected:0.######}, got {actual:0.######}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
