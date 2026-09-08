using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class CombatBatchOrderValidation
{
    public static void ValidateMixedSelectionBatchOrders()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x4241_5443_484F_5244L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var owner = galaxy.Civilizations[0];
        var enemy = galaxy.Civilizations[1];
        var battleSystem = galaxy.Systems[0];
        var remoteSystem = galaxy.Systems[1];

        var firstPatrol = CreateFleet(1001, owner.Id, "Batch Patrol One", FleetRole.Military, battleSystem.Id, battleSystem.Position, CombatProfileIds.PatrolCorvetteMk1);
        var secondPatrol = CreateFleet(1002, owner.Id, "Batch Patrol Two", FleetRole.Military, battleSystem.Id, battleSystem.Position, CombatProfileIds.PatrolCorvetteMk1);
        var scout = CreateFleet(1003, owner.Id, "Batch Scout", FleetRole.Scout, battleSystem.Id, battleSystem.Position, CombatProfileIds.CivilianLight);
        var remotePatrol = CreateFleet(1004, owner.Id, "Remote Patrol", FleetRole.Military, remoteSystem.Id, remoteSystem.Position, CombatProfileIds.PatrolCorvetteMk1);
        var target = CreateFleet(2000, enemy.Id, "Batch Hostile Target", FleetRole.Military, battleSystem.Id, battleSystem.Position, CombatProfileIds.PatrolCorvetteMk1);

        galaxy.Fleets.Add(firstPatrol);
        galaxy.Fleets.Add(secondPatrol);
        galaxy.Fleets.Add(scout);
        galaxy.Fleets.Add(remotePatrol);
        galaxy.Fleets.Add(target);

        var combat = new CombatSimulation(new DelegateCombatHostilityView((first, second) =>
            first == owner.Id && second == enemy.Id));
        var coordinator = new GalaxySimulationStepCoordinator(combat: combat);

        var batch = coordinator.IssueMilitaryOrders(
            galaxy,
            owner.Id,
            new[] { secondPatrol.Id, scout.Id, firstPatrol.Id, secondPatrol.Id, remotePatrol.Id, 99999 },
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));

        Require(batch.RequestedFleetCount == 5, "batch command did not de-duplicate repeated fleet identities");
        Require(batch.AcceptedCount == 2 && batch.RejectedCount == 3,
            $"mixed batch returned unexpected acceptance counts: {batch.AcceptedCount} accepted, {batch.RejectedCount} rejected");
        Require(batch.AnyAccepted && !batch.AllAccepted, "mixed batch did not expose partial-success state");
        Require(batch.FleetResults.Select(result => result.FleetId).SequenceEqual(new[] { 1001, 1002, 1003, 1004, 99999 }),
            "batch results were not deterministic and sorted by fleet identity");
        Require(batch.FleetResults.Single(result => result.FleetId == firstPatrol.Id).Accepted, "first valid patrol was rejected");
        Require(batch.FleetResults.Single(result => result.FleetId == secondPatrol.Id).Accepted, "second valid patrol was rejected");
        Require(!batch.FleetResults.Single(result => result.FleetId == scout.Id).Accepted, "unarmed scout accepted an attack order");
        Require(!batch.FleetResults.Single(result => result.FleetId == remotePatrol.Id).Accepted, "off-system patrol accepted an attack on a remote target");
        Require(!batch.FleetResults.Single(result => result.FleetId == 99999).Accepted, "unknown fleet identity disappeared instead of returning an explicit rejection");

        Require(CombatProfileRegistry.EnsureState(firstPatrol).Order == MilitaryOrderType.Attack &&
                CombatProfileRegistry.EnsureState(firstPatrol).TargetFleetId == target.Id,
            "first accepted patrol did not receive the authoritative attack order");
        Require(CombatProfileRegistry.EnsureState(secondPatrol).Order == MilitaryOrderType.Attack &&
                CombatProfileRegistry.EnsureState(secondPatrol).TargetFleetId == target.Id,
            "second accepted patrol did not receive the authoritative attack order");
        Require(CombatProfileRegistry.EnsureState(scout).Order == MilitaryOrderType.Hold,
            "rejected scout selection was mutated");
        Require(CombatProfileRegistry.EnsureState(remotePatrol).Order == MilitaryOrderType.Hold,
            "rejected off-system patrol was mutated");

        var holdBatch = coordinator.IssueMilitaryOrders(
            galaxy,
            owner.Id,
            new[] { secondPatrol.Id, firstPatrol.Id },
            new MilitaryOrder(MilitaryOrderType.Hold));
        Require(holdBatch.RequestedFleetCount == 2 && holdBatch.AcceptedCount == 2 && holdBatch.AllAccepted,
            "valid two-vessel Hold batch was not fully accepted");
        Require(CombatProfileRegistry.EnsureState(firstPatrol).Order == MilitaryOrderType.Hold &&
                CombatProfileRegistry.EnsureState(secondPatrol).Order == MilitaryOrderType.Hold,
            "batch Hold order did not clear both accepted patrol orders");

        var empty = coordinator.IssueMilitaryOrders(
            galaxy,
            owner.Id,
            Array.Empty<int>(),
            new MilitaryOrder(MilitaryOrderType.Hold));
        Require(empty.RequestedFleetCount == 0 && empty.AcceptedCount == 0 && empty.RejectedCount == 0 &&
                !empty.AnyAccepted && !empty.AllAccepted,
            "empty transient selection returned a misleading batch status");
    }

    private static FleetState CreateFleet(
        int id,
        int civilizationId,
        string name,
        FleetRole role,
        int systemId,
        Vector2 position,
        string combatProfileId) => new()
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
        Combat = CombatProfileRegistry.CreateInitialState(combatProfileId, role),
    };

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
