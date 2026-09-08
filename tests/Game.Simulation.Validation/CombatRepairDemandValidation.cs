using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class CombatRepairDemandValidation
{
    public static void ValidateNonMutatingCombatRepairDemand()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5245_5041_4952_444DL,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var civilization = galaxy.Civilizations[0];
        var foreignCivilization = galaxy.Civilizations[1];
        var system = galaxy.Systems[0];

        var pristineLegacy = CreateFleet(
            8100,
            civilization.Id,
            "Repair Pristine Legacy Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            combat: null);
        galaxy.Fleets.Add(pristineLegacy);

        var damagedPatrol = CreateFleet(
            8101,
            civilization.Id,
            "Repair Damaged Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        damagedPatrol.Combat!.Shields = 5.0;
        damagedPatrol.Combat.Armor = 20.0;
        damagedPatrol.Combat.Hull = 40.0;
        galaxy.Fleets.Add(damagedPatrol);

        var damagedColonyShip = CreateFleet(
            8102,
            civilization.Id,
            "Repair Damaged Colony Ship",
            FleetRole.Colony,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.CivilianHeavy, FleetRole.Colony));
        damagedColonyShip.Combat!.Armor = 12.0;
        damagedColonyShip.Combat.Hull = 80.0;
        galaxy.Fleets.Add(damagedColonyShip);

        var destroyed = CreateFleet(
            8103,
            civilization.Id,
            "Repair Destroyed Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        destroyed.IsActive = false;
        destroyed.Combat!.Hull = 0.0;
        galaxy.Fleets.Add(destroyed);

        var invalidProfile = CreateFleet(
            8104,
            civilization.Id,
            "Repair Unknown Profile",
            FleetRole.Military,
            system.Id,
            system.Position,
            new FleetCombatState
            {
                ProfileId = "unknown-profile",
                Shields = 0.0,
                Armor = 0.0,
                Hull = 1.0,
            });
        galaxy.Fleets.Add(invalidProfile);

        var foreignDamaged = CreateFleet(
            8200,
            foreignCivilization.Id,
            "Repair Foreign Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        foreignDamaged.Combat!.Hull = 20.0;
        galaxy.Fleets.Add(foreignDamaged);

        var patrolBefore = (damagedPatrol.Combat!.Shields, damagedPatrol.Combat.Armor, damagedPatrol.Combat.Hull);
        var colonyBefore = (damagedColonyShip.Combat!.Shields, damagedColonyShip.Combat.Armor, damagedColonyShip.Combat.Hull);
        var invalidBefore = invalidProfile.Combat!.ProfileId;

        var demand = CombatRepairDemandCalculator.Build(galaxy, civilization.Id);

        Require(demand.DamagedVesselCount == 2, "repair demand included pristine, destroyed, invalid-profile, or foreign vessels");
        Require(demand.HullDamagedVesselCount == 2, "hull-damaged vessel count was incorrect");
        Require(demand.Fleets.Select(fleet => fleet.FleetId).SequenceEqual(new[] { 8101, 8102 }),
            "repair demand was not deterministic by fleet ID");

        var patrolNeed = demand.Fleets[0];
        RequireNear(patrolNeed.MissingShields, 30.0, "patrol shield repair deficit changed");
        RequireNear(patrolNeed.MissingArmor, 25.0, "patrol armor repair deficit changed");
        RequireNear(patrolNeed.MissingHull, 55.0, "patrol hull repair deficit changed");
        RequireNear(patrolNeed.HullIntegrityRatio, 40.0 / 95.0, "patrol hull integrity ratio changed");
        Require(patrolNeed.HasStructuralDamage, "patrol hull damage was not marked structural");
        RequireNear(patrolNeed.TotalMissingDurability, 110.0, "patrol total repair deficit changed");

        var colonyNeed = demand.Fleets[1];
        RequireNear(colonyNeed.MissingShields, 0.0, "civilian-heavy profile invented shield repair demand");
        RequireNear(colonyNeed.MissingArmor, 12.0, "colony ship armor repair deficit changed");
        RequireNear(colonyNeed.MissingHull, 25.0, "colony ship hull repair deficit changed");
        RequireNear(colonyNeed.TotalMissingDurability, 37.0, "colony ship total repair deficit changed");

        RequireNear(demand.TotalMissingShields, 30.0, "civilization shield repair total changed");
        RequireNear(demand.TotalMissingArmor, 37.0, "civilization armor repair total changed");
        RequireNear(demand.TotalMissingHull, 80.0, "civilization hull repair total changed");
        RequireNear(demand.TotalMissingDurability, 147.0, "civilization total Combat repair demand changed");

        Require(pristineLegacy.Combat is null, "repair-demand read created Combat state on a pristine legacy fleet");
        Require(patrolBefore == (damagedPatrol.Combat!.Shields, damagedPatrol.Combat.Armor, damagedPatrol.Combat.Hull),
            "repair-demand read mutated patrol damage state");
        Require(colonyBefore == (damagedColonyShip.Combat!.Shields, damagedColonyShip.Combat.Armor, damagedColonyShip.Combat.Hull),
            "repair-demand read mutated colony-ship damage state");
        Require(invalidProfile.Combat!.ProfileId == invalidBefore,
            "repair-demand read replaced an unknown profile instead of remaining non-mutating");

        Require(CombatRepairDemandCalculator.BuildFleetDemand(pristineLegacy) is null,
            "pristine fleet produced repair demand");
        Require(CombatRepairDemandCalculator.BuildFleetDemand(destroyed) is null,
            "inactive destroyed fleet produced repair demand");
        Require(CombatRepairDemandCalculator.BuildFleetDemand(invalidProfile) is null,
            "unknown profile produced repair demand instead of following pristine-default semantics");

        RequireThrows(
            () => CombatRepairDemandCalculator.Build(galaxy, int.MaxValue),
            "unknown civilization repair-demand request was not rejected");
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
        StrategicSpeed = role == FleetRole.Military ? 21.0 : 13.5,
        SensorRange = role == FleetRole.Military ? 125.0f : 75.0f,
        IsActive = true,
        Combat = combat,
    };

    private static void RequireThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

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
